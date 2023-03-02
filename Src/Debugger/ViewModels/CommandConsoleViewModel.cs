using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using ReactiveUI;

using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Debugger.ViewModels.Helpers;
using Debugger.Views;
using Debugger.Windows;
using Debugger.ViewModels.Types;

namespace Debugger.ViewModels;

internal class CommandConsoleViewModel : ReactiveObject
{
    private readonly MainWindow? mainWindow;
    private readonly FormulaProgram? formulaProgram;
    private readonly AutoCompleteBox? commandInput;
    private readonly TextBlock? commandOutput;
    private readonly InferenceRulesViewModel? inferenceRulesViewModel;
    private readonly CurrentTermsViewModel? termsViewModel;
    private readonly ConstraintsViewModel? constraintsViewModel;
    private readonly SolverViewModel? solverViewModel;
    private readonly TextBlock? fileOutput;
    private List<Task> tasks = new List<Task>();

    private enum TaskType
    {
        INIT = 0,
        EXECUTE = 1,
        START = 2,
        COMMAND = 3,
        LOAD = 4
    }
    
    public CommandConsoleViewModel(MainWindow win, FormulaProgram program)
    {
        mainWindow = win;
        formulaProgram = program;

        commandInput = mainWindow.Get<AutoCompleteBox>("CommandInput");
        if (commandInput != null)
        {
            commandInput.KeyDown += InputKey;
        }
        
        fileOutput = mainWindow.Get<Domain4MLView>("DomainView")
                              .Get<TextBlock>("FileOutput");
        
        var commandInputView = mainWindow.Get<CommandConsoleView>("CommandInputView");
        commandOutput = commandInputView.Get<TextBlock>("ConsoleOutput");

        termsViewModel = mainWindow.Get<CurrentTermsView>("TermsView").DataContext as CurrentTermsViewModel;
        constraintsViewModel = mainWindow.Get<ConstraintsView>("ConstrView").DataContext as ConstraintsViewModel;
        inferenceRulesViewModel = mainWindow.Get<InferenceRulesView>("SolverRulesView").DataContext as InferenceRulesViewModel;
        solverViewModel = mainWindow.Get<SolverView>("SolverCommandView").DataContext as SolverViewModel;
    }

    private void InputKey(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            RunCmd();
        }
    }
    
    public void RunCmd()
    {
        if (mainWindow != null &&
            formulaProgram != null &&
            commandInput != null &&
            commandOutput != null)
        {
            if (commandInput.Text != null &&
                commandInput.Text.Length > 0)
            {
                if (commandInput.Text.StartsWith("load") ||
                    commandInput.Text.StartsWith("l"))
                {
                    formulaProgram.FormulaPublisher.ClearAll();
                    tasks.Clear();
                    
                    var cmdTask = new Task(() => LoadCommand(commandInput.Text));
                    cmdTask.Start();
                    var timeOutTask = new Task(() => TimeoutAfter(cmdTask, TaskType.LOAD));
                    timeOutTask.Start();
                    tasks.Add(timeOutTask);
                }
                else if (commandInput.Text.StartsWith("solve execute") ||
                         commandInput.Text.StartsWith("sl execute"))
                {
                    var exeTask = new Task(SolverExecute, TaskCreationOptions.LongRunning);
                    exeTask.Start();
                    var timeExeTask = new Task(() => TimeoutAfter(exeTask, TaskType.EXECUTE));
                    timeExeTask.Start();
                    tasks.Add(timeExeTask);
                }
                else if (commandInput.Text.StartsWith("solve init") ||
                         commandInput.Text.StartsWith("sl init"))
                {
                    var initSolveTask = new Task(SolverInit);
                    initSolveTask.Start();
                    var timeoutTask = new Task(() => TimeoutAfter(initSolveTask, TaskType.INIT));
                    timeoutTask.Start();
                    tasks.Add(timeoutTask);
                }
                else if ((commandInput.Text.StartsWith("solve") ||
                          commandInput.Text.StartsWith("sl")) &&
                         inferenceRulesViewModel != null)
                {
                    var commandExecuteTask = new Task(() => ExecuteCommand(commandInput.Text));
                    commandExecuteTask.Start();
                    var timeoutExecuteTask = new Task(() => TimeoutAfter(commandExecuteTask, TaskType.COMMAND));
                    timeoutExecuteTask.Start();
                    tasks.Add(timeoutExecuteTask);
                }
                else
                {
                    if (commandInput.Text.StartsWith("reload") ||
                        commandInput.Text.StartsWith("rl"))
                    {
                        formulaProgram.FormulaPublisher.ClearAll();
                        tasks.Clear();
                    }

                    var commandExecuteTask = new Task(() => ExecuteCommand(commandInput.Text));
                    commandExecuteTask.Start();
                    var timeoutTask = new Task(() => TimeoutAfter(commandExecuteTask, TaskType.COMMAND));
                    timeoutTask.Start();
                    tasks.Add(timeoutTask);
                }
            }
        }
    }
    
    private async void TimeoutAfter(Task task, TaskType type) 
    {
        if (formulaProgram != null)
        {
            using(var timeoutCancellationTokenSource = new CancellationTokenSource())
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (commandOutput != null)
                    {
                        if (commandOutput.Text == null ||
                            commandOutput.Text.Length <= 0)
                        {
                            commandOutput.Text += "[]> ";
                        }
                    }
                }, DispatcherPriority.Render);

                var timeout = new TimeSpan(0, 0, 0, 10);
                var completedTask = await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationTokenSource.Token));
                if (completedTask == task)
                {
                    timeoutCancellationTokenSource.Cancel();
                } 
                else
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        if (commandOutput != null)
                        {
                            commandOutput.Text += "\n";
                            commandOutput.Text += "Solve " + type + " task timed out.";
                            commandOutput.Text += "\n";
                            commandOutput.Text += "10s";
                            commandOutput.Text += "\n\n";
                            commandOutput.Text += "[]>";
                        }
                    }, DispatcherPriority.Render);
                }
            }
        }
    }

    private void LoadCommand(string input)
    {
        if (formulaProgram != null)
        {
            if (!formulaProgram.ExecuteCommand("unload *"))
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (commandOutput != null)
                    {
                        commandOutput.Text += "ERROR: " + TaskType.COMMAND + " failed to execute.";
                    }
                }, DispatcherPriority.Render);
                return;
            }

            formulaProgram.ClearConsoleOutput();

            var outP = "";
            if (input.StartsWith("load"))
            {
                outP = input.Replace("load ", "");
            }
            else if (input.StartsWith("l"))
            {
                outP = input.Replace("l ", "");
            }

            var fileOut = Utils.OpenFileText(outP);
            Dispatcher.UIThread.Post(() =>
            {
                if (fileOutput != null)
                {
                    fileOutput.Text = fileOut;
                }
            }, DispatcherPriority.Render);

            if (!formulaProgram.ExecuteCommand(input))
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (commandOutput != null)
                    {
                        commandOutput.Text += "ERROR: " + TaskType.COMMAND + " failed to execute.";
                    }
                }, DispatcherPriority.Render);
                return;
            }

            Dispatcher.UIThread.Post(() =>
            {
                if (commandOutput != null)
                {
                    commandOutput.Text += input;

                    foreach (var cmd in Utils.InputCommands)
                    {
                        if (input.StartsWith(cmd))
                        {
                            commandOutput.Text += "\n";
                        }
                    }
                    
                    commandOutput.Text += formulaProgram.GetConsoleOutput();
                }
            }, DispatcherPriority.Render);
        }
    }

    private void ExecuteCommand(string input)
    {
        if (formulaProgram != null)
        {
            if (!formulaProgram.ExecuteCommand(input))
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (commandOutput != null)
                    {
                        commandOutput.Text += "ERROR: " + TaskType.COMMAND + " failed to execute.";
                    }
                }, DispatcherPriority.Render);
                return;
            }

            Dispatcher.UIThread.Post(() =>
            {
                if (commandOutput != null &&
                    commandInput != null)
                {
                    commandOutput.Text += commandInput.Text;

                    foreach (var cmd in Utils.InputCommands)
                    {
                        if (commandInput.Text != null &&
                            commandInput.Text.StartsWith(cmd))
                        {
                            commandOutput.Text += "\n";
                        }
                    }
                    
                    commandOutput.Text += formulaProgram.GetConsoleOutput();
                }
            }, DispatcherPriority.Render);
        }
    }

    private void SolverInit()
    {
        if (formulaProgram != null)
        {
            var solveRes = formulaProgram.FormulaPublisher.GetSolverResult();
            if (solveRes != null)
            {
                solveRes.Init();

                var coreRules = formulaProgram.FormulaPublisher.GetCoreRules();
                var varFacts = formulaProgram.FormulaPublisher.GetVarFacts();
                var rules = formulaProgram.FormulaPublisher.GetCurrentTerms();
                var posConstraints = formulaProgram.FormulaPublisher.GetPosConstraints();
                var negConstraints = formulaProgram.FormulaPublisher.GetNegConstraints();
                var dirConstraints = formulaProgram.FormulaPublisher.GetDirConstraints();
                var flatConstraints = formulaProgram.FormulaPublisher.GetFlatConstraints();
                Dispatcher.UIThread.Post(() =>
                {
                    if (commandOutput != null &&
                        termsViewModel != null &&
                        constraintsViewModel != null &&
                        inferenceRulesViewModel != null &&
                        solverViewModel != null)
                    {
                        inferenceRulesViewModel.ClearAll();

                        if (coreRules != null)
                        {
                            foreach (var rulePair in coreRules)
                            {
                                foreach (var rule in rulePair.Value)
                                {
                                    var rn = new Node(rule);
                                    inferenceRulesViewModel.Items.Add(rn);
                                }
                            }
                        }
                        
                        if (varFacts.Count > 0)
                        {
                            solverViewModel.ClearAll();
                            
                            foreach (var varFact in varFacts)
                            {
                                var n = new Node(varFact.Value, varFact.Key);
                                solverViewModel.VariableItems.Add(n);
                            }
                            
                            EnableConstraintPanel();
                        }

                        termsViewModel.ClearAll();
                        constraintsViewModel.ClearAll();

                        var flag = true;
                        foreach (var term in rules)
                        {
                            var node = new Node(term.Value, term.Key);
                            termsViewModel.CurrentTermItems.Add(node);

                            if (flag)
                            {
                                if (dirConstraints.ContainsKey(term.Key))
                                {
                                    foreach (var v in dirConstraints[term.Key])
                                    {
                                        var dirn = new Node(v, term.Key);
                                        constraintsViewModel.DirectConstraintsItems.Add(dirn);
                                    }
                                }

                                if (posConstraints.ContainsKey(term.Key))
                                {
                                    foreach (var v in posConstraints[term.Key])
                                    {
                                        var posn = new Node(v, term.Key);
                                        constraintsViewModel.PosConstraintsItems.Add(posn);
                                    }
                                }

                                if (negConstraints.ContainsKey(term.Key))
                                {
                                    foreach (var v in negConstraints[term.Key])
                                    {
                                        var negn = new Node(v, term.Key);
                                        constraintsViewModel.NegConstraintsItems.Add(negn);
                                    }
                                }

                                if (flatConstraints.ContainsKey(term.Key))
                                {
                                    foreach (var v in flatConstraints[term.Key])
                                    {
                                        var flatn = new Node(v, term.Key);
                                        constraintsViewModel.FlatConstraintsItems.Add(flatn);
                                    }
                                }
                            }
                        }

                        if (commandOutput != null)
                        {
                            commandOutput.Text += "\n";
                            commandOutput.Text += "Solve " + TaskType.INIT + " task completed.";
                            commandOutput.Text += "\n\n";
                            commandOutput.Text += "[]>";
                        }
                    }
                }, DispatcherPriority.Render);
            }
        }
    }

    private void SolverExecute()
    {
        if (formulaProgram != null)
        {
            var solveRes = formulaProgram.FormulaPublisher.GetSolverResult();
            if (solveRes != null)
            {
                solveRes.Execute();
                
                var rules = formulaProgram.FormulaPublisher.GetCurrentTerms();
                var posConstraints = formulaProgram.FormulaPublisher.GetPosConstraints();
                var negConstraints = formulaProgram.FormulaPublisher.GetNegConstraints();
                var dirConstraints = formulaProgram.FormulaPublisher.GetDirConstraints();
                var flatConstraints = formulaProgram.FormulaPublisher.GetFlatConstraints();
                Dispatcher.UIThread.Post(() =>
                {
                    if (commandOutput != null &&
                        termsViewModel != null &&
                        constraintsViewModel != null)
                    {
                        termsViewModel.ClearAll();
                        constraintsViewModel.ClearAll();

                        var flag = true;
                        foreach (var term in rules)
                        {
                            var node = new Node(term.Value, term.Key);
                            termsViewModel.CurrentTermItems.Add(node);

                            if (flag)
                            {
                                if (dirConstraints.ContainsKey(term.Key))
                                {
                                    foreach (var v in dirConstraints[term.Key])
                                    {
                                        var dirn = new Node(v, term.Key);
                                        constraintsViewModel.DirectConstraintsItems.Add(dirn);
                                    }
                                }

                                if (posConstraints.ContainsKey(term.Key))
                                {
                                    foreach (var v in posConstraints[term.Key])
                                    {
                                        var posn = new Node(v, term.Key);
                                        constraintsViewModel.PosConstraintsItems.Add(posn);
                                    }
                                }

                                if (negConstraints.ContainsKey(term.Key))
                                {
                                    foreach (var v in negConstraints[term.Key])
                                    {
                                        var negn = new Node(v, term.Key);
                                        constraintsViewModel.NegConstraintsItems.Add(negn);
                                    }
                                }


                                if (flatConstraints.ContainsKey(term.Key))
                                {
                                    foreach (var v in flatConstraints[term.Key])
                                    {
                                        var flatn = new Node(v, term.Key);
                                        constraintsViewModel.FlatConstraintsItems.Add(flatn);
                                    }
                                }

                                flag = false;
                            }
                        }

                        if (commandOutput != null)
                        {
                            commandOutput.Text += "\n";
                            commandOutput.Text += "Solve " + TaskType.EXECUTE + " task completed.";
                            commandOutput.Text += "\n\n";
                            commandOutput.Text += "[]>";
                        }
                    }
                }, DispatcherPriority.Render);
            }
        }
    }

    private void SolverStart()
    {
        if (formulaProgram != null)
        {
            var solveRes = formulaProgram.FormulaPublisher.GetSolverResult();
            if (solveRes != null)
            {
                var startTime = DateTime.Now;
                    
                solveRes.Start();

                Dispatcher.UIThread.Post(() =>
                {
                    if (commandOutput != null)
                    {
                        commandOutput.Text += "\n";
                        commandOutput.Text += "Solve " + TaskType.START + " task completed. Solveable: " + solveRes.Solvable;
                        commandOutput.Text += "\n";
                        commandOutput.Text += (solveRes.StopTime - startTime).Milliseconds + "ms";
                        commandOutput.Text += "\n\n";
                        commandOutput.Text += "[]>";
                    }
                }, DispatcherPriority.Render);
            }
        }
    }

    public void StartSolve()
    {
        var startSolveTask = new Task(SolverStart, TaskCreationOptions.LongRunning);
        startSolveTask.Start();
        var timeoutStartTask = new Task(() => TimeoutAfter(startSolveTask, TaskType.START));
        timeoutStartTask.Start();
        tasks.Add(timeoutStartTask);
    }
    
    private void EnableConstraintPanel()
    {
        if (mainWindow != null)
        {
            mainWindow.Get<SolverView>("SolverCommandView").Get<ComboBox>("VariableSelection").IsEnabled = true;
            mainWindow.Get<SolverView>("SolverCommandView").Get<ComboBox>("ConstraintSelection").IsEnabled = true;
            mainWindow.Get<SolverView>("SolverCommandView").Get<TextBox>("InputExpression").IsEnabled = true;
            mainWindow.Get<SolverView>("SolverCommandView").Get<Button>("AddConstraintButton").IsEnabled = true;
        }
    }
}