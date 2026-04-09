namespace Singulink.UI.Navigation.Utilities;

internal static class ExpressionParser
{
    internal static string GetLambdaParameterName(string lambdaExpr)
    {
        int lambdaOpIndex = lambdaExpr.IndexOf("=>", StringComparison.Ordinal);

        if (lambdaOpIndex < 0)
            throw new ArgumentException("No lambda operator in expression.", nameof(lambdaExpr));

        ReadOnlySpan<char> paramsPart = lambdaExpr.AsSpan()[..lambdaOpIndex].Trim();

        if (paramsPart is ['(', .., ')'])
            paramsPart = paramsPart[1..^1].Trim();

        // For typed param "SomeType p", take last word; for untyped "p", take the entire thing
        int lastSpace = paramsPart.LastIndexOf(' ');
        return lastSpace >= 0 ? paramsPart[(lastSpace + 1)..].Trim().ToString() : paramsPart.ToString();
    }

    internal static List<string> GetParamNamesFromLambda(string lambdaExpr)
    {
        int lambdaOpIndex = lambdaExpr.IndexOf("=>", StringComparison.Ordinal);

        if (lambdaOpIndex < 0)
            throw new ArgumentException($"No lambda operator in expression.", nameof(lambdaExpr));

        ReadOnlySpan<char> paramsPart = lambdaExpr.AsSpan()[..lambdaOpIndex].Trim();

        if (paramsPart.Length is 0)
            throw new ArgumentException("Lambda expression does not contain a parameter section.", nameof(lambdaExpr));

        if (paramsPart[0] is '(')
        {
            if (paramsPart[^1] is not ')')
                throw new ArgumentException($"Unmatched opening brace for lambda parameters: '{paramsPart}'", nameof(lambdaExpr));

            paramsPart = paramsPart[1..^1];
        }

        return GetParamNames(paramsPart);
    }

    internal static List<string> GetParamNames(ReadOnlySpan<char> paramList)
    {
        List<string> paramNames = [];

        int genericDepth = 0;
        int tupleDepth = 0;
        int paramNameStartIndex = paramList.Length;
        bool startedType = false;

        for (int i = 0; i < paramList.Length; i++)
        {
            char c = paramList[i];

            if (c is '<')
            {
                genericDepth++;
            }
            else if (c is '>')
            {
                genericDepth--;
            }
            else if (c is '(')
            {
                tupleDepth++;
            }
            else if (c is ')')
            {
                tupleDepth--;
            }
            else if (c is ' ')
            {
                if (genericDepth is 0 && tupleDepth is 0 && startedType)
                {
                    while (paramList[++i] is ' ' && i < paramList.Length)
                    { }

                    paramNameStartIndex = i;

                    while (++i < paramList.Length && paramList[i] is not ',')
                    { }

                    var paramName = paramList[paramNameStartIndex..i].TrimEnd();

                    if (paramName.Length > 0)
                        paramNames.Add(paramName.ToString());

                    paramNameStartIndex = paramList.Length;

                    startedType = false;
                }
            }
            else
            {
                startedType = true;
            }
        }

        if (paramNameStartIndex < paramList.Length)
        {
            var paramName = paramList[paramNameStartIndex..].TrimEnd();

            if (paramName.Length > 0)
                paramNames.Add(paramName.ToString());
        }

        return paramNames;
    }
}
