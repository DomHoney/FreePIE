﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using FreePIE.Core.Common;
using FreePIE.Core.Contracts;
using FreePIE.Core.Plugins;
using FreePIE.Core.ScriptEngine.Globals;
using FreePIE.Core.ScriptEngine.Globals.ScriptHelpers;

namespace FreePIE.Core.ScriptEngine
{
    public class LuaScriptParser : IScriptParser
    {
        private readonly IPluginInvoker pluginInvoker;

        public LuaScriptParser(IPluginInvoker pluginInvoker)
        {
            this.pluginInvoker = pluginInvoker;
        }

        public IEnumerable<IPlugin> InvokeAndConfigureAllScriptDependantPlugins(string script)
        {
            var pluginTypes = pluginInvoker.ListAllPluginTypes()
                .Select(pt =>
                        new
                            {
                                Name = GlobalsInfo.GetGlobalName(pt),
                                PluginType = pt
                            }
                )
                .Where(info => script.Contains(info.Name))
                .Select(info => info.PluginType).ToList();

            return pluginInvoker.InvokeAndConfigurePlugins(pluginTypes);
        }

        public string PrepareScript(string script, IEnumerable<object> globals)
        {
            script = FindAndInitMethodsThatNeedIndexer(script, globals);
            return script;
        }

        private static Regex enumRegex = new Regex(@"(?<enum>[a-zA-Z0-9]*)\.(?<value>[a-zA-Z0-9]*)");

        private string FindAndInitMethodsThatNeedIndexer(string script, IEnumerable<object> globals)
        {
            var globalsThatNeedsIndex = globals
                .SelectMany(g => g.GetType().GetMethods()
                    .Where(m => m.GetCustomAttributes(typeof(NeedIndexer), false).Length > 0)
                    .Select(m => new { Global = g, MethodInfo = m }));
            
            foreach (var needIndex in globalsThatNeedsIndex)
            {
                var name = GlobalsInfo.GetGlobalName(needIndex.Global);
                var methodName = needIndex.MethodInfo.Name;
                var searchFor = string.Format("{0}:{1}", name, methodName);

                for (int i = 0; i < script.Length - searchFor.Length; i++)
                {
                    if(script.Substring(i, searchFor.Length) == searchFor)
                    {
                        int argumentStart = i + searchFor.Length;
                        var arguments = ExtractArguments(script, argumentStart);
                        int argumentEnd = argumentStart + arguments.Length;

                        var newArguments = string.Format(@"{0}, ""{1}"")", arguments.Substring(0, arguments.Length - 1), arguments.Substring(1, arguments.Length - 2).Replace(@"""", @"'" ));

                        script = script.Substring(0, argumentStart) +
                                 newArguments + script.Substring(argumentEnd, script.Length - argumentEnd);

                        i = argumentStart + newArguments.Length;
                    }
                }
            }

            return script;
        }

        private string ExtractArguments(string script, int start)
        {
            int parenthesesCount = 0;
            int index = start;
            do
            {
                if (script[index] == '(')
                    parenthesesCount++;

                if (script[index] == ')')
                    parenthesesCount--;

                index++;

            } while (parenthesesCount > 0 && index < script.Length);

            return script.Substring(start, index - start);
        }

        private static readonly char[] ExpressionDelimiters = "() \r\n".ToArray();
        private static readonly char[] TokenDelimiters = ".:".ToArray();

        private int GetStartOfExpression(string script, int offset)
        {
            for(int i = offset; i > 0; i--)
            {
                if (ExpressionDelimiters.Contains(script[i - 1]))
                    return i;
            }

            return 0;
        }

        public TokenResult GetTokensFromExpression(string script, int offset)
        {
            var tokens = new List<string>();

            int start = GetStartOfExpression(script, offset);

            var token = new StringBuilder();

            for(int i = start; i < offset; i++)
            {
                if(!TokenDelimiters.Contains(script[i]))
                    token.Append(script[i]);
                else tokens.Add(token.Extract());
            }

            tokens.Add(token.ToString());

            string lastToken = tokens.Last();

            return new TokenResult(tokens, new Range(offset - lastToken.Length, lastToken.Length));
        }
    }

    internal static class StringBuilderExtensions
    {
        public static string Extract(this StringBuilder builder)
        {
            string retVal = builder.ToString();

            builder.Length = 0;

            return retVal;
        }
    }
}
