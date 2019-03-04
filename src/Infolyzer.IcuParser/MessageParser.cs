using System.Collections.Generic;

// ReSharper disable ClassNeverInstantiated.Local

namespace Infolyzer.IcuParser
{
    public class MessageParser
    {
        public static string Parse(string value)
        {
            return Parse(value, new Dictionary<string, object>());
        }

        public static string Parse(string value, IDictionary<string, object> args)
        {
            var parser = new MessageParser(args);

            // State machine starts here:
            foreach (var symbol in value)
            {
                parser.Eval(symbol);
            }
            return parser.Result();
        }

        internal Stack<Scope> ScopeStack { get; }
        internal IDictionary<string, object> Variables { get; }

        private MessageParser(IDictionary<string, object> variables)
        {
            Variables = variables;
            ScopeStack = new Stack<Scope>();
            ScopeStack.Push(new Scope(this));
        }

        internal void Eval(char symbol)
        {
            var currentScopeBeforeEval = ScopeStack.Peek();
            var resultingState = currentScopeBeforeEval.State.Eval(symbol.ToString());
            var currentScopeAfterEval = ScopeStack.Peek();

            // Only update the state is we still are in the same scope as before the Eval.
            // Without this check, we might alter state on the parent scope when the child scope's state changes
            if (currentScopeAfterEval == currentScopeBeforeEval)
            {
                currentScopeAfterEval.State = resultingState;
            }
        }

        internal string Result()
        {
            if (ScopeStack.Count == 0)
            {
                throw new IcuParserException("Syntax error");
            }
            var scope = ScopeStack.Pop();
            if (ScopeStack.Count > 0)
            {
                throw new IcuParserException($"Scope stack mismatch: expected zero scopes left, but was {ScopeStack.Count}");
            }
            return scope.Result();
        }
    }
}