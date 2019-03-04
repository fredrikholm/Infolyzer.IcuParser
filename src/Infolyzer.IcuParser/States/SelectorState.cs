using System.Text.RegularExpressions;

namespace Infolyzer.IcuParser.States
{
    internal class SelectorState : BaseState
    {
        private int? _offset;

        public SelectorState() { }

        public SelectorState(int offset)
        {
            _offset = offset;
        }

        public override BaseState Eval(string c)
        {
            switch (c)
            {
                case "{":
                    var selectorToken = Scope.CurrentToken.Flush().Trim();
                    if (Scope.Command == "plural")
                    {
                        ValidateSelector(selectorToken);
                    }

                    // Start new child scope with state set to TextAppenderState
                    var branchScope = Scope.AddBranchScope(selectorToken);
                    branchScope.IsPluralBranch = true;
                    branchScope.Offset = _offset;
                    return branchScope.State;

                case "}":
                    // End of conditional
                    // Get rid of any potential white space characters
                    Scope.CurrentToken.Flush();

                    // Evaluate which one of the branches we should use
                    Scope.AddBranchResult();

                    // Go back to basic the text parsing state
                    return TransitionTo<TextAppenderState>();
            }

            if (Regex.IsMatch(Scope.CurrentToken + c, "\\s*offset\\s*:"))
            {
                Scope.CurrentToken.Clear();
                return TransitionTo<OffsetState>();
            }
            Scope.CurrentToken.Append(c);
            return this;
        }

        private void ValidateSelector(string token)
        {
            if (!Regex.IsMatch(token, "^((=((0)|([1-9]\\d*)))|(one)|(other))$", RegexOptions.IgnoreCase))
            {
                throw new IcuParserException($"Invalid selector: {token}");
            }
        }
    }

    internal class OffsetState : BaseState
    {
        public override BaseState Eval(string c)
        {
            if (Regex.IsMatch(Scope.CurrentToken + c, "^((0)|([1-9]\\d*))$"))
            {
                Scope.CurrentToken.Append(c);
                return this;
            }
            if (Regex.IsMatch(c, "\\s"))
            {
                var offset = int.Parse(Scope.CurrentToken.ToString());
                Scope.CurrentToken.Clear();
                return TransitionTo<SelectorState>(offset);
            }
            throw new IcuParserException($"Invalid offset value: {Scope.CurrentToken}{c}");
        }
    }
}
