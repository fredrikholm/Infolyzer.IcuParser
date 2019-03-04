using System.Collections.Generic;
using System.Text;
using Infolyzer.IcuParser.States;

namespace Infolyzer.IcuParser
{
    internal enum TokenType
    {
        Text,
        Quote,
        Variable,
        Branch,
        Unknown
    }

    internal class Token
    {
        public string Value { get; }
        public TokenType Type { get; }

        public Token(string value, TokenType type)
        {
            Value = value;
            Type = type;
        }
    }

    internal class Scope
    {
        private readonly MessageParser _parser;
        private readonly IList<Token> _tokens;
        private readonly IDictionary<string, Scope> _branches;
        private string _variable;

        public BaseState State { get; set; }
        public StringBuilder CurrentToken { get; }
        public string Command { get; private set; }
        public bool IsPluralBranch { get; set; }
        public int? Offset { get; set; }

        public Scope(MessageParser parser)
        {
            _parser = parser;
            _tokens = new List<Token>();
            _branches = new Dictionary<string, Scope>();

            CurrentToken = new StringBuilder();
            State = BaseState.Init<TextAppenderState>(this);
        }

        public void FlushCurrentToken(TokenType type)
        {
            var value = CurrentToken.Flush();
            if (string.IsNullOrEmpty(value))
            {
                return;
            }
            _tokens.Add(new Token(value, type));
        }

        public string Result(int? value = null)
        {
            if (!(State is TextAppenderState) && !(State is TentativeQuoteTerminationState))
            {
                if (State is EnterQuoteState || State is QuoteState)
                {
                    throw new IcuParserException("Unterminated quote");
                }
                throw new IcuParserException("Syntax error");
            }
            FlushCurrentToken(TokenType.Text);
            var sb = new StringBuilder();
            if (value.HasValue)
            {
                var hashReplacementNumber = value.Value - (Offset ?? 0);
                foreach (var token in _tokens)
                {
                    var tokenValue = token.Value;
                    if (token.Type == TokenType.Text)
                    {
                        tokenValue = tokenValue.Replace("#", hashReplacementNumber.ToString());
                    }
                    sb.Append(tokenValue);
                }
            }
            else
            {
                foreach (var token in _tokens)
                {
                    sb.Append(token.Value);
                }
            }
            return sb.ToString();
        }

        public Scope AddBranchScope(string selectorToken)
        {
            var branchScope = new Scope(_parser);
            _branches.Add(selectorToken, branchScope);
            _parser.ScopeStack.Push(branchScope);
            return branchScope;
        }

        public void SetCommand(string parsedToken)
        {
            var command = parsedToken.ToLower();
            if (command != "plural" && command != "select")
            {
                throw new IcuParserException($"Invalid command: {parsedToken}");
            }
            Command = command;
        }

        public void SetVariable(string parsedToken)
        {
            ValidateVariable(parsedToken);
            _variable = parsedToken;
        }

        public void AddVariableToken(string tokenValue)
        {
            ValidateVariable(tokenValue);
            var token = new Token(_parser.Variables[tokenValue].ToString(), TokenType.Variable);
            _tokens.Add(token);
        }

        private void ValidateVariable(string variableName)
        {
            if (!_parser.Variables.ContainsKey(variableName))
            {
                throw new IcuParserException($"Invalid variable: {variableName}");
            }
        }

        public SelectorState CloseChildScope()
        {
            // Get a hold of the parent scope
            if (_parser.ScopeStack.Count < 2)
            {
                throw new IcuParserException("Scope stack mismatch");
            }
            _parser.ScopeStack.Pop();
            var parentScope = _parser.ScopeStack.Peek();
            if (parentScope.State is SelectorState)
            {
                return (SelectorState)(parentScope.State);
            }
            throw new IcuParserException($"State mismatch after closing child scope: expected SelectorState, but was {parentScope.State.GetType().Name}");
        }

        public void AddBranchResult()
        {
            if (_branches.Count < 2)
            {
                throw new IcuParserException("Invalid conditional, should have at least 2 branches.");
            }
            var result = Command == "select" 
                ? GetSelectResult() 
                : GetPluralResult();
            if (!string.IsNullOrEmpty(result))
            {
                _tokens.Add(new Token(result, TokenType.Branch));
            }
            _branches.Clear();
            Command = null;
            _variable = null;
        }

        private string GetSelectResult()
        {
            var value = _parser.Variables[_variable].ToString();
            if (_branches.ContainsKey(value))
            {
                return _branches[value].Result();
            }
            return _branches.ContainsKey("other") 
                ? _branches["other"].Result() 
                : string.Empty;
        }

        private string GetPluralResult()
        {
            var value = int.Parse(_parser.Variables[_variable].ToString());
            foreach (var kvp in _branches)
            {
                var selector = kvp.Key;
                if (IsMatch(selector, value))
                {
                    var branchScope = _branches[selector];
                    return branchScope.Result(value);
                }
            }
            // No criteria is met
            return string.Empty;
        }

        private bool IsMatch(string selector, int value)
        {
            if (selector.Substring(0, 1) == "=")
            {
                var number = int.Parse(selector.Substring(1));
                return number == value;
            }
            if (selector == "one")
            {
                return value.ToString() == "1";
            }
            return selector == "other";
        }
    }
}
