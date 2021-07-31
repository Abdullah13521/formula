﻿namespace Microsoft.Formula.CommandLine
{
    using System;
    using System.Numerics;
    using System.Threading;
    using API;
    using Common;

    internal class CommandLineProgram
    {
        // dotnet publish CommandLine.csproj -c Release -r win-x64 --self-contained true
        public static void Main(string[] args)
        {
            var sink = new ConsoleSink();
            var chooser = new ConsoleChooser();
            var envParams = new EnvParams();
            var ci = new CommandInterface(sink, chooser, envParams);
            if (args.Length == 0) {
                Console.WriteLine("Please provide commands separated by '|'");
                return;
            }

            Console.WriteLine("Input commands: {0}", args[0]);

            // All commands must be wrapped in double quotes
            var args_str = args[0];
            var commands = args_str.Split("|");
            
            // Turn on wait on by default to run all commands synchronously
            ci.DoCommand("wait on");
            foreach (string command in commands)
            {
                Console.WriteLine("Executing command: {0}", command);
                ci.DoCommand(command);
            }
        }

        private class ConsoleChooser : IChooser
        {
            public ConsoleChooser()
            {
                Interactive = true;
            }

            public bool Interactive { get; set; }

            public bool GetChoice(out DigitChoiceKind choice)
            {
                if (!Interactive)
                {
                    choice = DigitChoiceKind.Zero;
                    return true;
                }

                var key = Console.ReadKey(true);
                switch (key.KeyChar)
                {
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                        choice = (DigitChoiceKind)(key.KeyChar - '0');
                        return true;
                    default:
                        choice = DigitChoiceKind.Zero;
                        return false;
                }
            }
        }

        private class ConsoleSink : IMessageSink
        {
            private bool printedErr = false;
            private SpinLock printedErrLock = new SpinLock();
            public bool PrintedError
            {
                get
                {
                    bool gotLock = false;
                    try
                    {
                        //// printedErrLock.Enter(ref gotLock);
                        return printedErr;
                    }
                    finally
                    {
                        if (gotLock)
                        {
                            //// printedErrLock.Exit();
                        }
                    }
                }
            }

            public System.IO.TextWriter Writer
            {
                get { return Console.Out; }
            }

            public void WriteMessage(string msg)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(msg);
                Console.ForegroundColor = ConsoleColor.Gray;
            }

            public void WriteMessage(string msg, API.SeverityKind severity)
            {
                switch (severity)
                {
                    case API.SeverityKind.Info:
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        break;
                    case API.SeverityKind.Warning:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        break;
                    case API.SeverityKind.Error:
                        SetPrintedError();
                        Console.ForegroundColor = ConsoleColor.Red;
                        break;
                    default:
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                }

                Console.Write(msg);
                Console.ForegroundColor = ConsoleColor.Gray;
            }

            public void WriteMessageLine(string msg)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(msg);
                Console.ForegroundColor = ConsoleColor.Gray;
            }

            public void WriteMessageLine(string msg, API.SeverityKind severity)
            {
                switch (severity)
                {
                    case API.SeverityKind.Info:
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        break;
                    case API.SeverityKind.Warning:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        break;
                    case API.SeverityKind.Error:
                        SetPrintedError();
                        Console.ForegroundColor = ConsoleColor.Red;
                        break;
                    default:
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                }

                Console.WriteLine(msg);
                Console.ForegroundColor = ConsoleColor.Gray;
            }

            private void SetPrintedError()
            {
                bool gotLock = false;
                try
                {
                    printedErrLock.Enter(ref gotLock);
                    printedErr = true;
                }
                finally
                {
                    if (gotLock)
                    {
                        printedErrLock.Exit();
                    }
                }
            }
        }
    }
}
