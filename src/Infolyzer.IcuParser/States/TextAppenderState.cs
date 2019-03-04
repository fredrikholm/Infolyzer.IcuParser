namespace Infolyzer.IcuParser.States
{
    internal class TextAppenderState : BaseState
    {
        public override BaseState Eval(string c)
        {
            switch (c)
            {
                case "'":
                    return TransitionTo<EnterQuoteState>();

                case "{":
                    return TransitionTo<ExpressionState>();

                case "}":
                    // Child scope ends here
                    Scope.FlushCurrentToken(TokenType.Text);
                    // Close current scope and return to parent scope, which is in SelectorState
                    return Scope.CloseChildScope();
            }

            Scope.CurrentToken.Append(c);
            return this;
        }
    }
}
