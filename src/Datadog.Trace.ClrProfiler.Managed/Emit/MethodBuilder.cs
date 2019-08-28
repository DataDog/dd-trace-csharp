using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using Datadog.Trace.ClrProfiler.Helpers;
using Datadog.Trace.Logging;
using Sigil;

namespace Datadog.Trace.ClrProfiler.Emit
{
    internal class MethodBuilder<TDelegate>
    {
        /// <summary>
        /// Global dictionary for caching reflected delegates
        /// </summary>
        private static readonly ConcurrentDictionary<Key, TDelegate> Cache = new ConcurrentDictionary<Key, TDelegate>(new KeyComparer());
        private static readonly ILog Log = LogProvider.GetLogger(typeof(MethodBuilder<TDelegate>));

        private readonly Module _resolutionModule;
        private readonly int _mdToken;
        private readonly int _originalOpCodeValue;
        private readonly OpCodeValue _opCode;
        private readonly string _methodName;
        private readonly Guid? _moduleVersionId;

        private Type _returnType;
        private MethodBase _methodBase;
        private Type _concreteType;
        private string _concreteTypeName;
        private object[] _parameters = new object[0];
        private Type[] _explicitParameterTypes = null;
        private string[] _namespaceAndNameFilter = null;
        private Type[] _declaringTypeGenerics;
        private Type[] _methodGenerics;
        private bool _forceMethodDefResolve;

        private MethodBuilder(Guid moduleVersionId, int mdToken, int opCode, string methodName)
            : this(ModuleLookup.Get(moduleVersionId), mdToken, opCode, methodName)
        {
            // Save the Guid for logging purposes
            _moduleVersionId = moduleVersionId;
        }

        private MethodBuilder(Module resolutionModule, int mdToken, int opCode, string methodName)
        {
            _resolutionModule = resolutionModule;
            _mdToken = mdToken;
            _opCode = (OpCodeValue)opCode;
            _originalOpCodeValue = opCode;
            _methodName = methodName;
            _forceMethodDefResolve = false;
        }

        public static MethodBuilder<TDelegate> Start(Guid moduleVersionId, int mdToken, int opCode, string methodName)
        {
            return new MethodBuilder<TDelegate>(moduleVersionId, mdToken, opCode, methodName);
        }

        public static MethodBuilder<TDelegate> Start(long moduleVersionPtr, int mdToken, int opCode, string methodName)
        {
            var ptr = new IntPtr(moduleVersionPtr);

#if NET45
            // deprecated
            var moduleVersionId = (Guid)Marshal.PtrToStructure(ptr, typeof(Guid));
#else
            // added in net451
            var moduleVersionId = Marshal.PtrToStructure<Guid>(ptr);
#endif

            return new MethodBuilder<TDelegate>(moduleVersionId, mdToken, opCode, methodName);
        }

        public MethodBuilder<TDelegate> WithConcreteType(Type type)
        {
            _concreteType = type;
            _concreteTypeName = type?.FullName;
            return this;
        }

        public MethodBuilder<TDelegate> WithNamespaceAndNameFilters(params string[] namespaceNameFilters)
        {
            _namespaceAndNameFilter = namespaceNameFilters;
            return this;
        }

        public MethodBuilder<TDelegate> WithParameters(params object[] parameters)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            _parameters = parameters;
            return this;
        }

        public MethodBuilder<TDelegate> WithExplicitParameterTypes(params Type[] types)
        {
            _explicitParameterTypes = types;
            return this;
        }

        public MethodBuilder<TDelegate> WithMethodGenerics(params Type[] generics)
        {
            _methodGenerics = generics;
            return this;
        }

        public MethodBuilder<TDelegate> WithDeclaringTypeGenerics(params Type[] generics)
        {
            _declaringTypeGenerics = generics;
            return this;
        }

        public MethodBuilder<TDelegate> ForceMethodDefinitionResolution()
        {
            _forceMethodDefResolve = true;
            return this;
        }

        public MethodBuilder<TDelegate> WithReturnType(Type returnType)
        {
            _returnType = returnType;
            return this;
        }

        public TDelegate Build()
        {
            var parameterTypesForCache = _explicitParameterTypes;

            if (parameterTypesForCache == null)
            {
                parameterTypesForCache = Interception.ParamsToTypes(_parameters);
            }

            var cacheKey = new Key(
                callingModule: _resolutionModule,
                mdToken: _mdToken,
                callOpCode: _opCode,
                concreteType: _concreteType,
                explicitParameterTypes: parameterTypesForCache,
                methodGenerics: _methodGenerics,
                declaringTypeGenerics: _declaringTypeGenerics);

            return Cache.GetOrAdd(cacheKey, key =>
            {
                // Validate requirements at the last possible moment
                // Don't do more than needed before checking the cache
                ValidateRequirements();
                return EmitDelegate();
            });
        }

        private TDelegate EmitDelegate()
        {
            var requiresBestEffortMatching = false;

            if (_resolutionModule != null)
            {
                try
                {
                    // Don't resolve until we build, as it may be an unnecessary lookup because of the cache
                    // We also may need the generics which were specified
                    if (_forceMethodDefResolve || (_declaringTypeGenerics == null && _methodGenerics == null))
                    {
                        _methodBase =
                            _resolutionModule.ResolveMethod(metadataToken: _mdToken);
                    }
                    else
                    {
                        _methodBase =
                            _resolutionModule.ResolveMethod(
                                metadataToken: _mdToken,
                                genericTypeArguments: _declaringTypeGenerics,
                                genericMethodArguments: _methodGenerics);
                    }
                }
                catch (Exception ex)
                {
                    string message = $"Unable to resolve method {_concreteTypeName}.{_methodName} by metadata token: {_mdToken}";
                    Log.Error(message, ex);
                    requiresBestEffortMatching = true;
                }
            }
            else
            {
                Log.Warn($"Unable to resolve module version id {_moduleVersionId}. Using method builder fallback.");
            }

            MethodInfo methodInfo = null;

            if (!requiresBestEffortMatching && _methodBase is MethodInfo info)
            {
                if (info.IsGenericMethodDefinition)
                {
                    info = MakeGenericMethod(info);
                }

                methodInfo = VerifyMethodFromToken(info);
            }

            if (methodInfo == null)
            {
                // mdToken didn't work out, fallback
                methodInfo = TryFindMethod();
            }

            Type delegateType = typeof(TDelegate);
            Type[] delegateGenericArgs = delegateType.GenericTypeArguments;

            Type[] delegateParameterTypes;
            Type returnType;

            if (delegateType.Name.StartsWith("Func`"))
            {
                // last generic type argument is the return type
                int parameterCount = delegateGenericArgs.Length - 1;
                delegateParameterTypes = new Type[parameterCount];
                Array.Copy(delegateGenericArgs, delegateParameterTypes, parameterCount);

                returnType = delegateGenericArgs[parameterCount];
            }
            else if (delegateType.Name.StartsWith("Action`"))
            {
                delegateParameterTypes = delegateGenericArgs;
                returnType = typeof(void);
            }
            else
            {
                throw new Exception($"Only Func<> or Action<> are supported in {nameof(MethodBuilder)}.");
            }

            if (methodInfo.IsGenericMethodDefinition)
            {
                methodInfo = MakeGenericMethod(methodInfo);
            }

            Type[] effectiveParameterTypes;

            var reflectedParameterTypes =
                methodInfo.GetParameters().Select(p => p.ParameterType);

            if (methodInfo.IsStatic)
            {
                effectiveParameterTypes = reflectedParameterTypes.ToArray();
            }
            else
            {
                // for instance methods, insert object's type as first element in array
                effectiveParameterTypes = new[] { _concreteType }
                                         .Concat(reflectedParameterTypes)
                                         .ToArray();
            }

            var dynamicMethod = Emit<TDelegate>.NewDynamicMethod(methodInfo.Name);

            // load each argument and cast or unbox as necessary
            for (ushort argumentIndex = 0; argumentIndex < delegateParameterTypes.Length; argumentIndex++)
            {
                Type delegateParameterType = delegateParameterTypes[argumentIndex];
                Type underlyingParameterType = effectiveParameterTypes[argumentIndex];

                dynamicMethod.LoadArgument(argumentIndex);

                if (underlyingParameterType.IsValueType && delegateParameterType == typeof(object))
                {
                    dynamicMethod.UnboxAny(underlyingParameterType);
                }
                else if (underlyingParameterType != delegateParameterType)
                {
                    dynamicMethod.CastClass(underlyingParameterType);
                }
            }

            if (_opCode == OpCodeValue.Call || methodInfo.IsStatic)
            {
                // non-virtual call (e.g. static method, or method override calling overriden implementation)
                dynamicMethod.Call(methodInfo);
            }
            else if (_opCode == OpCodeValue.Callvirt)
            {
                // Note: C# compiler uses CALLVIRT for non-virtual
                // instance methods to get the cheap null check
                dynamicMethod.CallVirtual(methodInfo);
            }
            else
            {
                throw new NotSupportedException($"OpCode {_originalOpCodeValue} not supported when calling a method.");
            }

            if (methodInfo.ReturnType.IsValueType && returnType == typeof(object))
            {
                dynamicMethod.Box(methodInfo.ReturnType);
            }
            else if (methodInfo.ReturnType != returnType)
            {
                dynamicMethod.CastClass(returnType);
            }

            dynamicMethod.Return();
            return dynamicMethod.CreateDelegate();
        }

        private MethodInfo MakeGenericMethod(MethodInfo methodInfo)
        {
            if (_methodGenerics == null || _methodGenerics.Length == 0)
            {
                throw new ArgumentException($"Must specify {nameof(_methodGenerics)} for a generic method.");
            }

            return methodInfo.MakeGenericMethod(_methodGenerics);
        }

        private MethodInfo VerifyMethodFromToken(MethodInfo methodInfo)
        {
            // Verify baselines to ensure this isn't the wrong method somehow
            var detailMessage = $"Unexpected method: {_concreteTypeName}.{_methodName} received for mdToken: {_mdToken} in module: {_resolutionModule?.FullyQualifiedName ?? "NULL"}, {_resolutionModule?.ModuleVersionId ?? _moduleVersionId}";

            if (!string.Equals(_methodName, methodInfo.Name))
            {
                Log.Warn($"Method name mismatch: {detailMessage}");
                return null;
            }

            if (!GenericsAreViable(methodInfo))
            {
                Log.Warn($"Generics not viable: {detailMessage}");
                return null;
            }

            if (!ParametersAreViable(methodInfo))
            {
                Log.Warn($"Parameters not viable: {detailMessage}");
                return null;
            }

            return methodInfo;
        }

        private void ValidateRequirements()
        {
            if (_concreteType == null)
            {
                throw new ArgumentException($"{nameof(_concreteType)} must be specified.");
            }

            if (string.IsNullOrWhiteSpace(_methodName))
            {
                throw new ArgumentException($"There must be a {nameof(_methodName)} specified to ensure fallback {nameof(TryFindMethod)} is viable.");
            }

            if (_namespaceAndNameFilter != null && _namespaceAndNameFilter.Length != _parameters.Length + 1)
            {
                throw new ArgumentException($"The length of {nameof(_namespaceAndNameFilter)} must match the length of {nameof(_parameters)} + 1 for the return type.");
            }

            if (_explicitParameterTypes != null)
            {
                if (_explicitParameterTypes.Length != _parameters.Length)
                {
                    throw new ArgumentException($"The {nameof(_explicitParameterTypes)} must match the {_parameters} count.");
                }

                for (var i = 0; i < _explicitParameterTypes.Length; i++)
                {
                    var explicitType = _explicitParameterTypes[i];
                    var parameterType = _parameters[i]?.GetType();

                    if (parameterType == null)
                    {
                        // Nothing to check
                        continue;
                    }

                    if (!explicitType.IsAssignableFrom(parameterType))
                    {
                        throw new ArgumentException($"Parameter Index {i}: Explicit type {explicitType.FullName} is not assignable from {parameterType}");
                    }
                }
            }
        }

        private MethodInfo TryFindMethod()
        {
            var logDetail = $"mdToken {_mdToken} on {_concreteTypeName}.{_methodName} in {_resolutionModule?.FullyQualifiedName ?? "NULL"}, {_resolutionModule?.ModuleVersionId ?? _moduleVersionId}";
            Log.Warn($"Using fallback method matching ({logDetail})");

            var methods =
                _concreteType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

            // A legacy fallback attempt to match on the concrete type
            methods =
                methods
                   .Where(mi => mi.Name == _methodName && (_returnType == null || mi.ReturnType == _returnType))
                   .ToArray();

            var matchesOnNameAndReturn = methods.Length;

            if (_namespaceAndNameFilter != null)
            {
                methods = methods.Where(m =>
                {
                    var parameters = m.GetParameters();

                    if ((parameters.Length + 1) != _namespaceAndNameFilter.Length)
                    {
                        return false;
                    }

                    var typesToCheck = new Type[] { m.ReturnType }.Concat(m.GetParameters().Select(p => p.ParameterType)).ToArray();
                    for (var i = 0; i < typesToCheck.Length; i++)
                    {
                        if (_namespaceAndNameFilter[i] == ClrNames.Ignore)
                        {
                            // Allow for not specifying
                            continue;
                        }

                        if ($"{typesToCheck[i].Namespace}.{typesToCheck[i].Name}" != _namespaceAndNameFilter[i])
                        {
                            return false;
                        }
                    }

                    return true;
                }).ToArray();
            }

            if (methods.Length == 1)
            {
                Log.Info($"Resolved by name and namespaceName filters ({logDetail})");
                return methods[0];
            }

            methods =
                methods
                   .Where(ParametersAreViable)
                   .ToArray();

            if (methods.Length == 1)
            {
                Log.Info($"Resolved by viable parameters ({logDetail})");
                return methods[0];
            }

            methods =
                methods
                   .Where(GenericsAreViable)
                   .ToArray();

            if (methods.Length == 1)
            {
                Log.Info($"Resolved by viable generics ({logDetail})");
                return methods[0];
            }

            // Attempt to trim down further
            methods = methods.Where(ParametersAreExact).ToArray();

            if (methods.Length > 1)
            {
                throw new ArgumentException($"Unable to safely resolve method, found {methods.Length} matches ({logDetail})");
            }

            var methodInfo = methods.SingleOrDefault();

            if (methodInfo == null)
            {
                throw new ArgumentException($"Unable to resolve method, started with {matchesOnNameAndReturn} by name match ({logDetail})");
            }

            return methodInfo;
        }

        private bool ParametersAreViable(MethodInfo mi)
        {
            var parameters = mi.GetParameters();

            if (parameters.Length != _parameters.Length)
            {
                // expected parameters don't match actual count
                return false;
            }

            for (var i = 0; i < parameters.Length; i++)
            {
                var candidateParameter = parameters[i];

                var parameterType = candidateParameter.ParameterType;

                var expectedParameterType = GetExpectedParameterTypeByIndex(i);

                if (expectedParameterType == null)
                {
                    // Skip the rest of this check, as we can't know the type
                    continue;
                }

                if (parameterType.IsGenericParameter)
                {
                    // This requires different evaluation
                    if (MeetsGenericArgumentRequirements(parameterType, expectedParameterType))
                    {
                        // Good to go
                        continue;
                    }

                    // We didn't meet this generic argument's requirements
                    return false;
                }

                if (!parameterType.IsAssignableFrom(expectedParameterType))
                {
                    return false;
                }
            }

            return true;
        }

        private bool ParametersAreExact(MethodInfo mi)
        {
            // We can already assume that the counts match by this point
            var parameters = mi.GetParameters();

            for (var i = 0; i < parameters.Length; i++)
            {
                var candidateParameter = parameters[i];

                var parameterType = candidateParameter.ParameterType;

                var actualArgumentType = GetExpectedParameterTypeByIndex(i);

                if (actualArgumentType == null)
                {
                    // Skip the rest of this check, as we can't know the type
                    continue;
                }

                if (parameterType != actualArgumentType)
                {
                    return false;
                }
            }

            return true;
        }

        private Type GetExpectedParameterTypeByIndex(int i)
        {
            return _explicitParameterTypes != null
                       ? _explicitParameterTypes[i]
                       : _parameters[i]?.GetType();
        }

        private bool GenericsAreViable(MethodInfo mi)
        {
            // Non-Generic Method - { IsGenericMethod: false, ContainsGenericParameters: false, IsGenericMethodDefinition: false }
            // Generic Method Definition - { IsGenericMethod: true, ContainsGenericParameters: true, IsGenericMethodDefinition: true }
            // Open Constructed Method - { IsGenericMethod: true, ContainsGenericParameters: true, IsGenericMethodDefinition: false }
            // Closed Constructed Method - { IsGenericMethod: true, ContainsGenericParameters: false, IsGenericMethodDefinition: false }

            if (_methodGenerics == null)
            {
                // We expect no generic arguments for this method
                return mi.ContainsGenericParameters == false;
            }

            if (!mi.IsGenericMethod)
            {
                // There is really nothing to compare here
                // Make sure we aren't looking for generics where there aren't
                return _methodGenerics?.Length == 0;
            }

            var genericArgs = mi.GetGenericArguments();

            if (genericArgs.Length != _methodGenerics.Length)
            {
                // Count of arguments mismatch
                return false;
            }

            foreach (var actualGenericArg in genericArgs)
            {
                if (actualGenericArg.IsGenericParameter)
                {
                    var expectedGenericArg = _methodGenerics[actualGenericArg.GenericParameterPosition];

                    if (!MeetsGenericArgumentRequirements(actualGenericArg, expectedGenericArg))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private bool MeetsGenericArgumentRequirements(Type actualGenericArg, Type expectedArg)
        {
            var constraints = actualGenericArg.GetGenericParameterConstraints();

            if (constraints.Any(constraint => !constraint.IsAssignableFrom(expectedArg)))
            {
                // We have failed to meet a constraint
                return false;
            }

            return true;
        }

        private struct Key
        {
            public readonly int CallingModuleMetadataToken;
            public readonly int MethodMetadataToken;
            public readonly OpCodeValue CallOpCode;
            public readonly string ConcreteTypeName;
            public readonly string GenericSpec;
            public readonly string ExplicitParams;

            public Key(
                Module callingModule,
                int mdToken,
                OpCodeValue callOpCode,
                Type concreteType,
                Type[] explicitParameterTypes,
                Type[] methodGenerics,
                Type[] declaringTypeGenerics)
            {
                CallingModuleMetadataToken = callingModule.MetadataToken;
                MethodMetadataToken = mdToken;
                CallOpCode = callOpCode;
                ConcreteTypeName = concreteType.AssemblyQualifiedName;

                GenericSpec = "_gArgs_";

                if (methodGenerics != null)
                {
                    for (var i = 0; i < methodGenerics.Length; i++)
                    {
                        GenericSpec = string.Concat(GenericSpec, $"_{methodGenerics[i]?.FullName ?? "NULL"}_");
                    }
                }

                GenericSpec = string.Concat(GenericSpec, "_gParams_");

                if (declaringTypeGenerics != null)
                {
                    for (var i = 0; i < declaringTypeGenerics.Length; i++)
                    {
                        GenericSpec = string.Concat(GenericSpec, $"_{declaringTypeGenerics[i]?.FullName ?? "NULL"}_");
                    }
                }

                ExplicitParams = string.Empty;

                if (explicitParameterTypes != null)
                {
                    ExplicitParams = string.Join("_", explicitParameterTypes.Select(ept => ept?.FullName ?? "NULL"));
                }
            }
        }

        private class KeyComparer : IEqualityComparer<Key>
        {
            public bool Equals(Key x, Key y)
            {
                if (!int.Equals(x.CallingModuleMetadataToken, y.CallingModuleMetadataToken))
                {
                    return false;
                }

                if (!int.Equals(x.MethodMetadataToken, y.MethodMetadataToken))
                {
                    return false;
                }

                if (!short.Equals(x.CallOpCode, y.CallOpCode))
                {
                    return false;
                }

                if (!string.Equals(x.ConcreteTypeName, y.ConcreteTypeName))
                {
                    return false;
                }

                if (!string.Equals(x.ExplicitParams, y.ExplicitParams))
                {
                    return false;
                }

                if (!string.Equals(x.GenericSpec, y.GenericSpec))
                {
                    return false;
                }

                return true;
            }

            public int GetHashCode(Key obj)
            {
                unchecked
                {
                    int hash = 17;
                    hash = (hash * 23) + obj.CallingModuleMetadataToken.GetHashCode();
                    hash = (hash * 23) + obj.MethodMetadataToken.GetHashCode();
                    hash = (hash * 23) + obj.CallOpCode.GetHashCode();
                    hash = (hash * 23) + obj.ConcreteTypeName.GetHashCode();
                    hash = (hash * 23) + obj.GenericSpec.GetHashCode();
                    hash = (hash * 23) + obj.ExplicitParams.GetHashCode();
                    return hash;
                }
            }
        }
    }
}
