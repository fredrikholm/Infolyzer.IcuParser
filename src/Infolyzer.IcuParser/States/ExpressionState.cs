using System.Text.RegularExpressions;

namespace Infolyzer.IcuParser.States
{
    internal class ExpressionState : BaseState
    {
        public override BaseState Eval(string c)
        {
            switch (c)
            {
                case "}":
                    // Expression was just a simple variable
                    Scope.AddVariableToken(Scope.CurrentToken.Flush().Trim());
                    return TransitionTo<TextAppenderState>();

                case ",":
                    // Expression is a conditional
                    var variable = Scope.CurrentToken.Flush().Trim();
                    if (!Regex.IsMatch(variable, "[a-z0-9_]*"))
                    {
                        throw new IcuParserException($"Invalid characters in selector variable: {variable}");
                    }
                    Scope.SetVariable(variable);
                    return TransitionTo<CommandState>();
            }

            Scope.CurrentToken.Append(c);
            return this;
        }
    }
}
