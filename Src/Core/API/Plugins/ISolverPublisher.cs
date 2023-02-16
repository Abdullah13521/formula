using System;
using System.Collections.Generic;
using Microsoft.Formula.Common;
using Microsoft.Formula.Common.Terms;

using System.Threading.Tasks;

namespace Microsoft.Formula.API.Plugins;

public interface ISolverPublisher
{
    public void SetVarFacts(Set<Term> facts);
    public void SetPosConstraintTerms(Set<Term> terms);
    public void SetNegConstraintTerms(Set<Term> terms);
    public void SetSolverResult(List<Task<SolveResult>> tasks);
    public void SetStartTime(DateTime time);

    public void SetLeastFixedPointMap(Map<Term, Term> lfp);
    public Set<Term> GetConstraintTerms();
    public SolveResult WaitForCompletion();
}