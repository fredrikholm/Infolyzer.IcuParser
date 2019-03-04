namespace Infolyzer.IcuParser.States
{
    internal class CommandState : BaseState
    {
        public override BaseState Eval(string c)
        {
            if (c == ",")
            {
                var parsedToken = Scope.CurrentToken.Flush().Trim();
                Scope.SetCommand(parsedToken);
                return TransitionTo<SelectorState>();
            }
            Scope.CurrentToken.Append(c);
            return this;
        }
    }
}
