using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Datadog.Trace.ClrProfiler.Emit;
using Datadog.Trace.Logging;

namespace Datadog.Trace.ClrProfiler.Integrations.AdoNet
{
    /// <summary>
    /// Instrumentation wrappers for <see cref="DbCommand"/>.
    /// </summary>
    public static class DbCommandIntegration
    {
        // TODO: support both "DbCommand" (new name) and
        // "AdoNet" (backwards compatibility) when reading configuration settings
        private const string IntegrationName = "AdoNet";
        private const string Major4 = "4";

        private static readonly Vendors.Serilog.ILogger Log = DatadogLogging.GetLogger(typeof(DbCommandIntegration));

        /// <summary>
        /// Instrumentation wrapper for <see cref="DbCommand.ExecuteReader()"/>.
        /// </summary>
        /// <param name="command">The object referenced "this" in the instrumented method.</param>
        /// <param name="opCode">The OpCode used in the original method call.</param>
        /// <param name="mdToken">The mdToken of the original method call.</param>
        /// <param name="moduleVersionPtr">A pointer to the module version GUID.</param>
        /// <returns>The value returned by the instrumented method.</returns>
        [InterceptMethod(
            TargetAssemblies = new[] { AdoNetConstants.AssemblyNames.SystemData, AdoNetConstants.AssemblyNames.SystemDataCommon, AdoNetConstants.AssemblyNames.NetStandard },
            TargetMethod = AdoNetConstants.MethodNames.ExecuteReader,
            TargetType = AdoNetConstants.TypeNames.DbCommand,
            TargetSignatureTypes = new[] { AdoNetConstants.TypeNames.DbDataReader },
            TargetMinimumVersion = Major4,
            TargetMaximumVersion = Major4)]
        public static object ExecuteReader(
            object command,
            int opCode,
            int mdToken,
            long moduleVersionPtr)
        {
            return ExecuteReaderInternal(
                command,
                opCode,
                mdToken,
                moduleVersionPtr,
                methodName: AdoNetConstants.MethodNames.ExecuteReader,
                returnTypeName: AdoNetConstants.TypeNames.DbDataReader);
        }

        /// <summary>
        /// Instrumentation wrapper for explicit implementation of <see cref="IDbCommand.ExecuteReader()"/> in <see cref="DbCommand"/>.
        /// </summary>
        /// <param name="command">The object referenced "this" in the instrumented method.</param>
        /// <param name="opCode">The OpCode used in the original method call.</param>
        /// <param name="mdToken">The mdToken of the original method call.</param>
        /// <param name="moduleVersionPtr">A pointer to the module version GUID.</param>
        /// <returns>The value returned by the instrumented method.</returns>
        [InterceptMethod(
            TargetAssemblies = new[] { AdoNetConstants.AssemblyNames.SystemData, AdoNetConstants.AssemblyNames.SystemDataCommon, AdoNetConstants.AssemblyNames.NetStandard },
            TargetMethod = AdoNetConstants.MethodNames.ExecuteReaderExplicit,
            TargetType = AdoNetConstants.TypeNames.DbCommand,
            TargetSignatureTypes = new[] { AdoNetConstants.TypeNames.IDataReader },
            TargetMinimumVersion = Major4,
            TargetMaximumVersion = Major4)]
        public static object ExecuteReaderExplicit(
            object command,
            int opCode,
            int mdToken,
            long moduleVersionPtr)
        {
            return ExecuteReaderInternal(
                command,
                opCode,
                mdToken,
                moduleVersionPtr,
                methodName: AdoNetConstants.MethodNames.ExecuteReaderExplicit,
                returnTypeName: AdoNetConstants.TypeNames.IDataReader);
        }

        private static object ExecuteReaderInternal(object command, int opCode, int mdToken, long moduleVersionPtr, string methodName, string returnTypeName)
        {
            Func<DbCommand, DbDataReader> instrumentedMethod;

            try
            {
                instrumentedMethod =
                    MethodBuilder<Func<DbCommand, DbDataReader>>
                       .Start(moduleVersionPtr, mdToken, opCode, methodName)
                       .WithConcreteType(typeof(DbCommand))
                       .WithNamespaceAndNameFilters(returnTypeName)
                       .Build();
            }
            catch (Exception ex)
            {
                Log.ErrorRetrievingMethod(
                    exception: ex,
                    moduleVersionPointer: moduleVersionPtr,
                    mdToken: mdToken,
                    opCode: opCode,
                    instrumentedType: AdoNetConstants.TypeNames.DbCommand,
                    methodName: nameof(ExecuteReader),
                    instanceType: command.GetType().AssemblyQualifiedName);
                throw;
            }

            var dbCommand = command as DbCommand;

            using (var scope = ScopeFactory.CreateDbCommandScope(Tracer.Instance, dbCommand, IntegrationName))
            {
                try
                {
                    return instrumentedMethod(dbCommand);
                }
                catch (Exception ex)
                {
                    scope?.Span.SetException(ex);
                    throw;
                }
            }
        }

        /// <summary>
        /// Instrumentation wrapper for <see cref="DbCommand.ExecuteReader(CommandBehavior)"/>.
        /// </summary>
        /// <param name="command">The object referenced "this" in the instrumented method.</param>
        /// <param name="behavior">The <see cref="CommandBehavior"/> value used in the original method call.</param>
        /// <param name="opCode">The OpCode used in the original method call.</param>
        /// <param name="mdToken">The mdToken of the original method call.</param>
        /// <param name="moduleVersionPtr">A pointer to the module version GUID.</param>
        /// <returns>The value returned by the instrumented method.</returns>
        [InterceptMethod(
            TargetAssemblies = new[] { AdoNetConstants.AssemblyNames.SystemData, AdoNetConstants.AssemblyNames.SystemDataCommon, AdoNetConstants.AssemblyNames.NetStandard },
            TargetMethod = AdoNetConstants.MethodNames.ExecuteReader,
            TargetType = AdoNetConstants.TypeNames.DbCommand,
            TargetSignatureTypes = new[] { AdoNetConstants.TypeNames.DbDataReader, AdoNetConstants.TypeNames.CommandBehavior },
            TargetMinimumVersion = Major4,
            TargetMaximumVersion = Major4)]
        public static object ExecuteReaderWithBehavior(
            object command,
            int behavior,
            int opCode,
            int mdToken,
            long moduleVersionPtr)
        {
            string methodName = AdoNetConstants.MethodNames.ExecuteReader;
            string returnTypeName = AdoNetConstants.TypeNames.DbDataReader;
            var commandBehavior = (CommandBehavior)behavior;

            return ExecuteReaderWithBehaviorInternal(
                command,
                commandBehavior,
                opCode,
                mdToken,
                moduleVersionPtr,
                methodName,
                returnTypeName);
        }

        /// <summary>
        /// Instrumentation wrapper for <see cref="DbCommand.ExecuteReader(CommandBehavior)"/>.
        /// </summary>
        /// <param name="command">The object referenced "this" in the instrumented method.</param>
        /// <param name="behavior">The <see cref="CommandBehavior"/> value used in the original method call.</param>
        /// <param name="opCode">The OpCode used in the original method call.</param>
        /// <param name="mdToken">The mdToken of the original method call.</param>
        /// <param name="moduleVersionPtr">A pointer to the module version GUID.</param>
        /// <returns>The value returned by the instrumented method.</returns>
        [InterceptMethod(
            TargetAssemblies = new[] { AdoNetConstants.AssemblyNames.SystemData, AdoNetConstants.AssemblyNames.SystemDataCommon, AdoNetConstants.AssemblyNames.NetStandard },
            TargetMethod = AdoNetConstants.MethodNames.ExecuteReaderExplicit,
            TargetType = AdoNetConstants.TypeNames.DbCommand,
            TargetSignatureTypes = new[] { AdoNetConstants.TypeNames.IDataReader, AdoNetConstants.TypeNames.CommandBehavior },
            TargetMinimumVersion = Major4,
            TargetMaximumVersion = Major4)]
        public static object ExecuteReaderWithBehaviorInternal(
            object command,
            int behavior,
            int opCode,
            int mdToken,
            long moduleVersionPtr)
        {
            string methodName = AdoNetConstants.MethodNames.ExecuteReaderExplicit;
            string returnTypeName = AdoNetConstants.TypeNames.IDataReader;
            var commandBehavior = (CommandBehavior)behavior;

            return ExecuteReaderWithBehaviorInternal(
                command,
                commandBehavior,
                opCode,
                mdToken,
                moduleVersionPtr,
                methodName,
                returnTypeName);
        }

        private static object ExecuteReaderWithBehaviorInternal(object command, CommandBehavior commandBehavior, int opCode, int mdToken, long moduleVersionPtr, string methodName, string returnTypeName)
        {
            Func<DbCommand, CommandBehavior, DbDataReader> instrumentedMethod;


            try
            {
                instrumentedMethod =
                    MethodBuilder<Func<DbCommand, CommandBehavior, DbDataReader>>
                       .Start(moduleVersionPtr, mdToken, opCode, methodName)
                       .WithConcreteType(typeof(DbCommand))
                       .WithParameters(commandBehavior)
                       .WithNamespaceAndNameFilters(returnTypeName, AdoNetConstants.TypeNames.CommandBehavior)
                       .Build();
            }
            catch (Exception ex)
            {
                Log.ErrorRetrievingMethod(
                    exception: ex,
                    moduleVersionPointer: moduleVersionPtr,
                    mdToken: mdToken,
                    opCode: opCode,
                    instrumentedType: AdoNetConstants.TypeNames.DbCommand,
                    methodName: nameof(ExecuteReader),
                    instanceType: command.GetType().AssemblyQualifiedName);
                throw;
            }

            var dbCommand = command as DbCommand;

            using (var scope = ScopeFactory.CreateDbCommandScope(Tracer.Instance, dbCommand, IntegrationName))
            {
                try
                {
                    return instrumentedMethod(dbCommand, commandBehavior);
                }
                catch (Exception ex)
                {
                    scope?.Span.SetException(ex);
                    throw;
                }
            }
        }

        /// <summary>
        /// Instrumentation wrapper for <see cref="DbCommand.ExecuteReaderAsync()"/>.
        /// </summary>
        /// <param name="command">The object referenced by this in the instrumented method.</param>
        /// <param name="behavior">The <see cref="CommandBehavior"/> value used in the original method call.</param>
        /// <param name="boxedCancellationToken">The <see cref="CancellationToken"/> value used in the original method call.</param>
        /// <param name="opCode">The OpCode used in the original method call.</param>
        /// <param name="mdToken">The mdToken of the original method call.</param>
        /// <param name="moduleVersionPtr">A pointer to the module version GUID.</param>
        /// <returns>The value returned by the instrumented method.</returns>
        [InterceptMethod(
            TargetAssemblies = new[] { AdoNetConstants.AssemblyNames.SystemData, AdoNetConstants.AssemblyNames.SystemDataCommon, AdoNetConstants.AssemblyNames.NetStandard },
            TargetType = AdoNetConstants.TypeNames.DbCommand,
            TargetSignatureTypes = new[] { "System.Threading.Tasks.Task`1<System.Data.Common.DbDataReader>", AdoNetConstants.TypeNames.CommandBehavior, ClrNames.CancellationToken },
            TargetMinimumVersion = Major4,
            TargetMaximumVersion = Major4)]
        public static object ExecuteReaderAsync(
            object command,
            int behavior,
            object boxedCancellationToken,
            int opCode,
            int mdToken,
            long moduleVersionPtr)
        {
            var cancellationToken = (CancellationToken)boxedCancellationToken;

            return ExecuteReaderAsyncInternal(
                command as DbCommand,
                (CommandBehavior)behavior,
                cancellationToken,
                opCode,
                mdToken,
                moduleVersionPtr);
        }

        private static async Task<DbDataReader> ExecuteReaderAsyncInternal(
            DbCommand command,
            CommandBehavior commandBehavior,
            CancellationToken cancellationToken,
            int opCode,
            int mdToken,
            long moduleVersionPtr)
        {
            Func<DbCommand, CommandBehavior, CancellationToken, Task<DbDataReader>> instrumentedMethod;

            try
            {
                instrumentedMethod =
                    MethodBuilder<Func<DbCommand, CommandBehavior, CancellationToken, Task<DbDataReader>>>
                       .Start(moduleVersionPtr, mdToken, opCode, AdoNetConstants.MethodNames.ExecuteReaderAsync)
                       .WithConcreteType(typeof(DbCommand))
                       .WithParameters(commandBehavior, cancellationToken)
                       .WithNamespaceAndNameFilters(ClrNames.GenericTask, AdoNetConstants.TypeNames.CommandBehavior, ClrNames.CancellationToken)
                       .Build();
            }
            catch (Exception ex)
            {
                Log.ErrorRetrievingMethod(
                    exception: ex,
                    moduleVersionPointer: moduleVersionPtr,
                    mdToken: mdToken,
                    opCode: opCode,
                    instrumentedType: AdoNetConstants.TypeNames.DbCommand,
                    methodName: nameof(ExecuteReaderAsync),
                    instanceType: command.GetType().AssemblyQualifiedName);
                throw;
            }

            using (var scope = ScopeFactory.CreateDbCommandScope(Tracer.Instance, command, IntegrationName))
            {
                try
                {
                    return await instrumentedMethod(command, commandBehavior, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    scope?.Span.SetException(ex);
                    throw;
                }
            }
        }

        /// <summary>
        /// Instrumentation wrapper for <see cref="DbCommand.ExecuteNonQuery"/>.
        /// </summary>
        /// <param name="command">The object referenced by this in the instrumented method.</param>
        /// <param name="opCode">The OpCode used in the original method call.</param>
        /// <param name="mdToken">The mdToken of the original method call.</param>
        /// <param name="moduleVersionPtr">A pointer to the module version GUID.</param>
        /// <returns>The value returned by the instrumented method.</returns>
        [InterceptMethod(
            TargetAssemblies = new[] { AdoNetConstants.AssemblyNames.SystemData, AdoNetConstants.AssemblyNames.SystemDataCommon, AdoNetConstants.AssemblyNames.NetStandard },
            TargetMethod = AdoNetConstants.MethodNames.ExecuteNonQuery,
            TargetType = AdoNetConstants.TypeNames.DbCommand,
            TargetSignatureTypes = new[] { ClrNames.Int32 },
            TargetMinimumVersion = Major4,
            TargetMaximumVersion = Major4)]
        [InterceptMethod(
            TargetAssemblies = new[] { AdoNetConstants.AssemblyNames.SystemData, AdoNetConstants.AssemblyNames.SystemDataCommon, AdoNetConstants.AssemblyNames.NetStandard },
            TargetMethod = AdoNetConstants.MethodNames.ExecuteNonQueryExplicit,
            TargetType = AdoNetConstants.TypeNames.DbCommand,
            TargetSignatureTypes = new[] { ClrNames.Int32 },
            TargetMinimumVersion = Major4,
            TargetMaximumVersion = Major4)]
        public static int ExecuteNonQuery(
            object command,
            int opCode,
            int mdToken,
            long moduleVersionPtr)
        {
            Func<DbCommand, int> instrumentedMethod;

            try
            {
                instrumentedMethod =
                    MethodBuilder<Func<DbCommand, int>>
                       .Start(moduleVersionPtr, mdToken, opCode, AdoNetConstants.MethodNames.ExecuteNonQuery)
                       .WithConcreteType(typeof(DbCommand))
                       .WithNamespaceAndNameFilters(ClrNames.Int32)
                       .Build();
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error resolving {AdoNetConstants.TypeNames.DbCommand}.{AdoNetConstants.MethodNames.ExecuteNonQuery}(...)");
                throw;
            }

            var dbCommand = command as DbCommand;

            using (var scope = ScopeFactory.CreateDbCommandScope(Tracer.Instance, dbCommand, IntegrationName))
            {
                try
                {
                    return instrumentedMethod(dbCommand);
                }
                catch (Exception ex)
                {
                    scope?.Span.SetException(ex);
                    throw;
                }
            }
        }

        /// <summary>
        /// Instrumentation wrapper for <see cref="DbCommand.ExecuteNonQueryAsync(CancellationToken)"/>
        /// </summary>
        /// <param name="command">The object referenced by this in the instrumented method.</param>
        /// <param name="boxedCancellationToken">The <see cref="CancellationToken"/> value used in the original method call.</param>
        /// <param name="opCode">The OpCode used in the original method call.</param>
        /// <param name="mdToken">The mdToken of the original method call.</param>
        /// <param name="moduleVersionPtr">A pointer to the module version GUID.</param>
        /// <returns>The value returned by the instrumented method.</returns>
        [InterceptMethod(
            TargetAssemblies = new[] { AdoNetConstants.AssemblyNames.SystemData, AdoNetConstants.AssemblyNames.SystemDataCommon, AdoNetConstants.AssemblyNames.NetStandard },
            TargetType = AdoNetConstants.TypeNames.DbCommand,
            TargetSignatureTypes = new[] { "System.Threading.Tasks.Task`1<System.Int32>", ClrNames.CancellationToken },
            TargetMinimumVersion = Major4,
            TargetMaximumVersion = Major4)]
        public static object ExecuteNonQueryAsync(
            object command,
            object boxedCancellationToken,
            int opCode,
            int mdToken,
            long moduleVersionPtr)
        {
            var cancellationToken = (CancellationToken)boxedCancellationToken;

            return ExecuteNonQueryAsyncInternal(
                command as DbCommand,
                cancellationToken,
                opCode,
                mdToken,
                moduleVersionPtr);
        }

        private static async Task<int> ExecuteNonQueryAsyncInternal(
            DbCommand command,
            CancellationToken cancellationToken,
            int opCode,
            int mdToken,
            long moduleVersionPtr)
        {
            Func<DbCommand, CancellationToken, Task<int>> instrumentedMethod;

            try
            {
                instrumentedMethod =
                    MethodBuilder<Func<DbCommand, CancellationToken, Task<int>>>
                       .Start(moduleVersionPtr, mdToken, opCode, AdoNetConstants.MethodNames.ExecuteNonQueryAsync)
                       .WithConcreteType(typeof(DbCommand))
                       .WithParameters(cancellationToken)
                       .WithNamespaceAndNameFilters(ClrNames.GenericTask, ClrNames.CancellationToken)
                       .Build();
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error resolving {AdoNetConstants.TypeNames.DbCommand}.{AdoNetConstants.MethodNames.ExecuteNonQueryAsync}(...)");
                throw;
            }

            using (var scope = ScopeFactory.CreateDbCommandScope(Tracer.Instance, command, IntegrationName))
            {
                try
                {
                    return await instrumentedMethod(command, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    scope?.Span.SetException(ex);
                    throw;
                }
            }
        }

        /// <summary>
        /// Instrumentation wrapper for <see cref="DbCommand.ExecuteScalar"/>
        /// </summary>
        /// <param name="command">The object referenced by this in the instrumented method.</param>
        /// <param name="opCode">The OpCode used in the original method call.</param>
        /// <param name="mdToken">The mdToken of the original method call.</param>
        /// <param name="moduleVersionPtr">A pointer to the module version GUID.</param>
        /// <returns>The value returned by the instrumented method.</returns>
        [InterceptMethod(
            TargetAssemblies = new[] { AdoNetConstants.AssemblyNames.SystemData, AdoNetConstants.AssemblyNames.SystemDataCommon, AdoNetConstants.AssemblyNames.NetStandard },
            TargetMethod = AdoNetConstants.MethodNames.ExecuteScalar,
            TargetType = AdoNetConstants.TypeNames.DbCommand,
            TargetSignatureTypes = new[] { ClrNames.Object },
            TargetMinimumVersion = Major4,
            TargetMaximumVersion = Major4)]
        [InterceptMethod(
            TargetAssemblies = new[] { AdoNetConstants.AssemblyNames.SystemData, AdoNetConstants.AssemblyNames.SystemDataCommon, AdoNetConstants.AssemblyNames.NetStandard },
            TargetMethod = AdoNetConstants.MethodNames.ExecuteScalarExplicit,
            TargetType = AdoNetConstants.TypeNames.DbCommand,
            TargetSignatureTypes = new[] { ClrNames.Object },
            TargetMinimumVersion = Major4,
            TargetMaximumVersion = Major4)]
        public static object ExecuteScalar(
            object command,
            int opCode,
            int mdToken,
            long moduleVersionPtr)
        {
            Func<DbCommand, object> instrumentedMethod;

            try
            {
                instrumentedMethod =
                    MethodBuilder<Func<DbCommand, object>>
                       .Start(moduleVersionPtr, mdToken, opCode, AdoNetConstants.MethodNames.ExecuteScalar)
                       .WithConcreteType(typeof(DbCommand))
                       .WithNamespaceAndNameFilters(ClrNames.Object)
                       .Build();
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error resolving {AdoNetConstants.TypeNames.DbCommand}.{AdoNetConstants.MethodNames.ExecuteScalar}(...)");
                throw;
            }

            var dbCommand = command as DbCommand;

            using (var scope = ScopeFactory.CreateDbCommandScope(Tracer.Instance, dbCommand, IntegrationName))
            {
                try
                {
                    return instrumentedMethod(dbCommand);
                }
                catch (Exception ex)
                {
                    scope?.Span.SetException(ex);
                    throw;
                }
            }
        }

        /// <summary>
        /// Instrumentation wrapper for <see cref="DbCommand.ExecuteScalarAsync(CancellationToken)"/>
        /// </summary>
        /// <param name="command">The object referenced by this in the instrumented method.</param>
        /// <param name="boxedCancellationToken">The <see cref="CancellationToken"/> value used in the original method call.</param>
        /// <param name="opCode">The OpCode used in the original method call.</param>
        /// <param name="mdToken">The mdToken of the original method call.</param>
        /// <param name="moduleVersionPtr">A pointer to the module version GUID.</param>
        /// <returns>The value returned by the instrumented method.</returns>
        [InterceptMethod(
            TargetAssemblies = new[] { AdoNetConstants.AssemblyNames.SystemData, AdoNetConstants.AssemblyNames.SystemDataCommon, AdoNetConstants.AssemblyNames.NetStandard },
            TargetType = AdoNetConstants.TypeNames.DbCommand,
            TargetSignatureTypes = new[] { "System.Threading.Tasks.Task`1<System.Object>", ClrNames.CancellationToken },
            TargetMinimumVersion = Major4,
            TargetMaximumVersion = Major4)]
        public static object ExecuteScalarAsync(
            object command,
            object boxedCancellationToken,
            int opCode,
            int mdToken,
            long moduleVersionPtr)
        {
            var cancellationToken = (CancellationToken)boxedCancellationToken;

            return ExecuteScalarAsyncInternal(
                command as DbCommand,
                cancellationToken,
                opCode,
                mdToken,
                moduleVersionPtr);
        }

        private static async Task<object> ExecuteScalarAsyncInternal(
            DbCommand command,
            CancellationToken cancellationToken,
            int opCode,
            int mdToken,
            long moduleVersionPtr)
        {
            Func<DbCommand, CancellationToken, Task<object>> instrumentedMethod;

            try
            {
                instrumentedMethod =
                    MethodBuilder<Func<DbCommand, CancellationToken, Task<object>>>
                       .Start(moduleVersionPtr, mdToken, opCode, AdoNetConstants.MethodNames.ExecuteScalarAsync)
                       .WithConcreteType(typeof(DbCommand))
                       .WithParameters(cancellationToken)
                       .WithNamespaceAndNameFilters(ClrNames.GenericTask, ClrNames.CancellationToken)
                       .Build();
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error resolving {AdoNetConstants.TypeNames.DbCommand}.{AdoNetConstants.MethodNames.ExecuteScalarAsync}(...)");
                throw;
            }

            using (var scope = ScopeFactory.CreateDbCommandScope(Tracer.Instance, command, IntegrationName))
            {
                try
                {
                    return await instrumentedMethod(command, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    scope?.Span.SetException(ex);
                    throw;
                }
            }
        }
    }
}
