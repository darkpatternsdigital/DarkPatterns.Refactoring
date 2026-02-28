using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace DarkPatterns.Refactoring.Attributes;

public static class AttributeDataExtensions
{
    extension(ISymbol symbol)
    {
        internal IEnumerable<T> FindAttributes<T>(Action<Diagnostic> reportDiagnostic) where T : class
        {
            var type = typeof(T);
            return symbol.GetAttributes().Where(ad => ad.AttributeClass?.Name == type.Name)
                .Select(ad => ad.CreateFromAttributeData<T>(symbol.Locations.First(), reportDiagnostic))
                .OfType<T>();
        }

        internal T? FindAttribute<T>(Action<Diagnostic> reportDiagnostic) where T : class
        {
            return symbol.FindAttributes<T>(reportDiagnostic).FirstOrDefault();
        }
    }

    extension(AttributeData ad)
    {
        // Source - https://stackoverflow.com/a/79583292/195653
        // Posted by Mohammad Hamdy, modified by community. See post 'Timeline' for change history
        // Retrieved 2026-02-06, License - CC BY-SA 4.0
        internal T? CreateFromAttributeData<T>(Location location, Action<Diagnostic> reportDiagnostic)
            where T : class
        {
            var type = typeof(T);

            Debug.Assert(type.Name == ad.AttributeClass?.Name, "Invalid attribute class name");

            var constParams = ad.AttributeConstructor?.Parameters ?? ImmutableArray<IParameterSymbol>.Empty;

            var typeConstructors = type.GetConstructors();

            System.Reflection.ConstructorInfo? cons = null;

            List<Type> paramTypes = new List<Type>();
            foreach (var con in typeConstructors)
            {
                bool constFound = true;

                var ps = con.GetParameters();

                paramTypes.Clear();

                if (ps.Length != constParams.Length)
                    constFound = false;
                else
                    for (int i = 0; i < constParams.Length; i++)
                    {
                        var p = ps[i];
                        var constParam = constParams[i];
                        paramTypes.Add(p.ParameterType);
                        if (p.Name != constParam.Name)
                        {
                            constFound = false;
                            break;
                        }

                    }
                if (constFound)
                {
                    cons = con;
                    break;
                }
            }

            if (cons == null)
            {
                reportDiagnostic(Diagnostic.Create(VersionMismatch.Rule, location));
                return null;
            }


            var constArgs = ad.ConstructorArguments;
            List<object?> args = new();

            int j = 0;
            for (int i = 0; i < constParams.Length; i++)
            {
                var constParam = constParams[i];

                object? constArg;

                if (i < constArgs.Length)
                {
                    if (constParams[i].Type.Kind != SymbolKind.ArrayType)
                    {
                        constArg = constArgs[j].Value;
                    }
                    else if (constParams[i].Type.Kind == SymbolKind.ArrayType
                        && constArgs[j].Kind == TypedConstantKind.Array)
                    {
                        var values = constArgs[j].Values;

                        var arrArg = Array.CreateInstance(
                            paramTypes[i].GetElementType(), values.Length);

                        for (int k = 0; k < values.Length; k++)
                        {
                            arrArg.SetValue(values[k].Value, k);
                        }

                        constArg = arrArg;
                    }
                    else if (constParam.Type.Kind == SymbolKind.ArrayType &&
                        constParam.IsParams)
                    {
                        var constValue = constArgs[j].Value;
                        if (constValue == null)
                        {
                            reportDiagnostic(Diagnostic.Create(VersionMismatch.Rule, location));
                            return null;
                        }
                        //get all arguments with the same type that are adjacent to this
                        //argument
                        List<object> paramArgs = [constValue];
                        while (j + 1 < constArgs.Length && SymbolEqualityComparer.Default.Equals(constArgs[j + 1].Type, constArgs[j].Type))
                        {
                            j = j + 1;
                            paramArgs.Add(constValue);
                        }
                        var typedParamArgs = Array.CreateInstance(paramArgs[0].GetType(), paramArgs.Count);

                        Array.Copy(paramArgs.ToArray(), typedParamArgs, paramArgs.Count);

                        constArg = typedParamArgs;
                    }
                    else
                    {
                        reportDiagnostic(Diagnostic.Create(VersionMismatch.Rule, location));
                        return null;
                    }
                }
                else if (constParam.IsParams)
                {
                    constArg = Array.CreateInstance(
                            paramTypes[i].GetElementType(), 0);
                }
                else if (constParam.HasExplicitDefaultValue)//create default value
                {
                    constArg = constParam.ExplicitDefaultValue;
                }
                else
                {
                    reportDiagnostic(Diagnostic.Create(VersionMismatch.Rule, location));
                    return null;
                }
                args.Add(constArg);
                j++;
            }

            T newAttr;
            try
            {
                newAttr = (T)cons.Invoke(args.ToArray());
            }
            catch (Exception)
            {
                reportDiagnostic(Diagnostic.Create(VersionMismatch.Rule, location));
                return null;
            }


            var propDic = ad.NamedArguments.ToImmutableSortedDictionary();

            var props = type.GetProperties();
            foreach (var prop in props)
            {
                if (propDic.ContainsKey(prop.Name) == false)
                    continue;

                var propVal = propDic[prop.Name];
                if (propVal.Kind != TypedConstantKind.Array)
                    prop.SetValue(newAttr, propVal.Value);
                else
                {
                    Array valArray = Array.CreateInstance(prop.PropertyType.GetElementType(), propVal.Values.Length);
                    for (int i = 0; i < valArray.Length; i++)
                        valArray.SetValue(propVal.Values[i].Value, i);
                    prop.SetValue(newAttr, valArray);
                }
            }

            return newAttr;

        }
    }
}