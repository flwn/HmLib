using Fixie;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace HmLib.Tests._Infrastructure
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class InputAttribute : Attribute
    {
        public InputAttribute(params object[] parameters)
        {
            Parameters = parameters;
        }

        public object[] Parameters { get; }
    }

    public class ParameterizedTestConvention : Convention
    {
        public ParameterizedTestConvention()
        {
            Classes
            .NameEndsWith("Tests");

            Methods
                .Where(method => method.IsVoid());

            Parameters
                .Add<FromInputAttributes>();
        }

        class FromInputAttributes : ParameterSource
        {
            public IEnumerable<object[]> GetParameters(MethodInfo method) =>
                 method
                    .GetCustomAttributes<InputAttribute>(true)
                    .Select(input => input.Parameters);
        }
    }
}
