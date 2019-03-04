using System;

namespace Infolyzer.IcuParser.States
{
    internal abstract class BaseState
    {
        protected Scope Scope { get; private set; }

        public abstract BaseState Eval(string c);

        protected BaseState TransitionTo<T>(params object[] args) where T : BaseState
        {
            TokenType tokenType;
            switch (this)
            {
                case TextAppenderState _:
                    tokenType = TokenType.Text;
                    break;
                case EnterQuoteState _:
                case QuoteState _:
                case TentativeQuoteTerminationState _: // Could also be text, see below
                    tokenType = TokenType.Quote;
                    break;
                default:
                    tokenType = TokenType.Unknown;
                    break;
            }
            if (this is TentativeQuoteTerminationState && typeof(T).Name == typeof(TextAppenderState).Name)
            {
                tokenType = TokenType.Text;
            }
            Scope.FlushCurrentToken(tokenType);
            return Init<T>(Scope, args);
        }

        internal static BaseState Init<T>(Scope scope, params object[] args) where T : BaseState
        {
            var state = (T)Activator.CreateInstance(typeof(T), args);
            state.Scope = scope;
            return state;
        }
    }
}
