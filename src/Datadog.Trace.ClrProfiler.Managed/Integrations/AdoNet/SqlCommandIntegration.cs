using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Datadog.Trace.ClrProfiler.Emit;
using Datadog.Trace.ClrProfiler.Helpers;
using Datadog.Trace.Logging;

namespace Datadog.Trace.ClrProfiler.Integrations.AdoNet
{
    /// <summary>
    /// Instrumentation wrappers for SqlCommand.
    /// </summary>
    public static class SqlCommandIntegration
    {
        private const string Major4 = "4";
        private const string Major5 = "4";

        private const string SystemSqlAssemblyName = "System.Data.SqlClient";
        private const string SystemSqlCommandTypeName = "System.Data.SqlClient.SqlCommand";
        private const string SystemSqlDataReaderTypeName = "System.Data.SqlClient.SqlDataReader";

        private static readonly Vendors.Serilog.ILogger Log = DatadogLogging.GetLogger(typeof(SqlCommandIntegration));

        /// <summary>
        /// Instrumentation wrapper for SqlCommand.ExecuteReader().
        /// </summary>
        /// <param name="command">The object referenced by this in the instrumented method.</param>
        /// <param name="opCode">The OpCode used in the original method call.</param>
        /// <param name="mdToken">The mdToken of the original method call.</param>
        /// <param name="moduleVersionPtr">A pointer to the module version GUID.</param>
        /// <returns>The value returned by the instrumented method.</returns>
        [InterceptMethod(
            TargetAssemblies = new[] { AdoNetConstants.AssemblyNames.SystemData, SystemSqlAssemblyName },
            TargetType = SystemSqlCommandTypeName,
            TargetMethod = AdoNetConstants.MethodNames.ExecuteReader,
            TargetSignatureTypes = new[] { SystemSqlDataReaderTypeName },
            TargetMinimumVersion = Major4,
            TargetMaximumVersion = Major5)]
        public static object ExecuteReader(
            object command,
            int opCode,
            int mdToken,
            long moduleVersionPtr)
        {
            const string methodName = AdoNetConstants.MethodNames.ExecuteReader;
            Func<object, object> instrumentedMethod;

            try
            {
                var targetType = command.GetInstrumentedType(SystemSqlCommandTypeName);

                instrumentedMethod =
                    MethodBuilder<Func<object, object>>
                       .Start(moduleVersionPtr, mdToken, opCode, methodName)
                       .WithConcreteType(targetType)
                       .WithNamespaceAndNameFilters(SystemSqlDataReaderTypeName)
                       .Build();
            }
            catch (Exception ex)
            {
                Log.ErrorRetrievingMethod(
                    exception: ex,
                    moduleVersionPointer: moduleVersionPtr,
                    mdToken: mdToken,
                    opCode: opCode,
                    instrumentedType: SystemSqlCommandTypeName,
                    methodName: methodName,
                    instanceType: command.GetType().AssemblyQualifiedName);
                throw;
            }

            using (var scope = ScopeFactory.CreateDbCommandScope(Tracer.Instance, command as DbCommand))
            {
                try
                {
                    return instrumentedMethod(command);
                }
                catch (Exception ex)
                {
                    scope?.Span.SetException(ex);
                    throw;
                }
            }
        }

        /// <summary>
        /// Instrumentation wrapper for SqlCommand.ExecuteReader().
        /// </summary>
        /// <param name="command">The object referenced by this in the instrumented method.</param>
        /// <param name="behavior">The <see cref="CommandBehavior"/> value used in the original method call.</param>
        /// <param name="opCode">The OpCode used in the original method call.</param>
        /// <param name="mdToken">The mdToken of the original method call.</param>
        /// <param name="moduleVersionPtr">A pointer to the module version GUID.</param>
        /// <returns>The value returned by the instrumented method.</returns>
        [InterceptMethod(
            TargetAssemblies = new[] { AdoNetConstants.AssemblyNames.SystemData, SystemSqlAssemblyName },
            TargetType = SystemSqlCommandTypeName,
            TargetMethod = AdoNetConstants.MethodNames.ExecuteReader,
            TargetSignatureTypes = new[] { SystemSqlDataReaderTypeName, AdoNetConstants.TypeNames.CommandBehavior },
            TargetMinimumVersion = Major4,
            TargetMaximumVersion = Major5)]
        public static object ExecuteReaderWithBehavior(
            object command,
            int behavior,
            int opCode,
            int mdToken,
            long moduleVersionPtr)
        {
            const string methodName = AdoNetConstants.MethodNames.ExecuteReader;
            var commandBehavior = (CommandBehavior)behavior;
            Func<object, CommandBehavior, object> instrumentedMethod;

            try
            {
                var targetType = command.GetInstrumentedType(SystemSqlCommandTypeName);

                instrumentedMethod =
                    MethodBuilder<Func<object, CommandBehavior, object>>
                       .Start(moduleVersionPtr, mdToken, opCode, methodName)
                       .WithConcreteType(targetType)
                       .WithParameters(commandBehavior)
                       .WithNamespaceAndNameFilters(SystemSqlDataReaderTypeName, AdoNetConstants.TypeNames.CommandBehavior)
                       .Build();
            }
            catch (Exception ex)
            {
                Log.ErrorRetrievingMethod(
                    exception: ex,
                    moduleVersionPointer: moduleVersionPtr,
                    mdToken: mdToken,
                    opCode: opCode,
                    instrumentedType: SystemSqlCommandTypeName,
                    methodName: methodName,
                    instanceType: command.GetType().AssemblyQualifiedName);
                throw;
            }

            using (var scope = ScopeFactory.CreateDbCommandScope(Tracer.Instance, command as DbCommand))
            {
                try
                {
                    return instrumentedMethod(command, commandBehavior);
                }
                catch (Exception ex)
                {
                    scope?.Span.SetException(ex);
                    throw;
                }
            }
        }

        /// <summary>
        /// Instrumentation wrapper for SqlCommand.ExecuteReaderAsync().
        /// </summary>
        /// <param name="command">The object referenced by this in the instrumented method.</param>
        /// <param name="behavior">The <see cref="CommandBehavior"/> value used in the original method call.</param>
        /// <param name="boxedCancellationToken">The <see cref="CancellationToken"/> value used in the original method call.</param>
        /// <param name="opCode">The OpCode used in the original method call.</param>
        /// <param name="mdToken">The mdToken of the original method call.</param>
        /// <param name="moduleVersionPtr">A pointer to the module version GUID.</param>
        /// <returns>The value returned by the instrumented method.</returns>
        [InterceptMethod(
            TargetAssemblies = new[] { AdoNetConstants.AssemblyNames.SystemData, SystemSqlAssemblyName },
            TargetType = SystemSqlCommandTypeName,
            TargetSignatureTypes = new[] { "System.Threading.Tasks.Task`1<System.Data.SqlClient.SqlDataReader>", AdoNetConstants.TypeNames.CommandBehavior, ClrNames.CancellationToken },
            TargetMinimumVersion = Major4,
            TargetMaximumVersion = Major5)]
        public static object ExecuteReaderAsync(
            object command,
            int behavior,
            object boxedCancellationToken,
            int opCode,
            int mdToken,
            long moduleVersionPtr)
        {
            const string methodName = AdoNetConstants.MethodNames.ExecuteReaderAsync;
            var cancellationToken = (CancellationToken)boxedCancellationToken;
            var commandBehavior = (CommandBehavior)behavior;

            Type sqlCommandType;
            Type sqlDataReaderType;

            try
            {
                sqlCommandType = command.GetInstrumentedType(SystemSqlCommandTypeName);
                sqlDataReaderType = sqlCommandType.Assembly.GetType(SystemSqlDataReaderTypeName);
            }
            catch (Exception ex)
            {
                // This shouldn't happen because the assembly holding the System.Data.SqlClient.SqlDataReader type should have been loaded already
                // profiled app will not continue working as expected without this method
                Log.Error(ex, "Error finding the System.Data.SqlClient.SqlDataReader type");
                throw;
            }

            Func<DbCommand, CommandBehavior, CancellationToken, object> instrumentedMethod;

            try
            {
                instrumentedMethod =
                    MethodBuilder<Func<DbCommand, CommandBehavior, CancellationToken, object>>
                       .Start(moduleVersionPtr, mdToken, opCode, methodName)
                       .WithConcreteType(sqlCommandType)
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
                    instrumentedType: SystemSqlCommandTypeName,
                    methodName: methodName,
                    instanceType: command.GetType().AssemblyQualifiedName);
                throw;
            }

            return AsyncHelper.InvokeGenericTaskDelegate(
                owningType: command.GetType(),
                taskResultType: sqlDataReaderType,
                nameOfIntegrationMethod: nameof(ExecuteReaderAsyncInternal),
                integrationType: typeof(SqlCommandIntegration),
                (DbCommand)command,
                commandBehavior,
                cancellationToken,
                instrumentedMethod);
        }

        /// <summary>
        /// Calls the underlying ExecuteReaderAsync and traces the request.
        /// </summary>
        /// <typeparam name="T">The type of the generic Task instantiation</typeparam>
        /// <param name="command">The object referenced by this in the instrumented method.</param>
        /// <param name="commandBehavior">The <see cref="CommandBehavior"/> value used in the original method call.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <param name="instrumentedMethod">A delegate for the method we are instrumenting</param>
        /// <returns>A task with the result</returns>
        private static async Task<T> ExecuteReaderAsyncInternal<T>(
            DbCommand command,
            CommandBehavior commandBehavior,
            CancellationToken cancellationToken,
            Func<DbCommand, CommandBehavior, CancellationToken, object> instrumentedMethod)
        {
            using (var scope = ScopeFactory.CreateDbCommandScope(Tracer.Instance, command))
            {
                try
                {
                    var task = (Task<T>)instrumentedMethod(command, commandBehavior, cancellationToken);
                    return await task.ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    scope?.Span.SetException(ex);
                    throw;
                }
            }
        }

        /// <summary>
        /// Instrumentation wrapper for SqlCommand.ExecuteNonQuery().
        /// </summary>
        /// <param name="command">The object referenced by this in the instrumented method.</param>
        /// <param name="opCode">The OpCode used in the original method call.</param>
        /// <param name="mdToken">The mdToken of the original method call.</param>
        /// <param name="moduleVersionPtr">A pointer to the module version GUID.</param>
        /// <returns>The value returned by the instrumented method.</returns>
        [InterceptMethod(
            TargetAssemblies = new[] { AdoNetConstants.AssemblyNames.SystemData, SystemSqlAssemblyName },
            TargetType = SystemSqlCommandTypeName,
            TargetSignatureTypes = new[] { ClrNames.Int32 },
            TargetMinimumVersion = Major4,
            TargetMaximumVersion = Major5)]
        public static int ExecuteNonQuery(
            object command,
            int opCode,
            int mdToken,
            long moduleVersionPtr)
        {
            const string methodName = AdoNetConstants.MethodNames.ExecuteNonQuery;
            Func<DbCommand, int> instrumentedMethod;

            try
            {
                var targetType = command.GetInstrumentedType(SystemSqlCommandTypeName);

                instrumentedMethod =
                    MethodBuilder<Func<DbCommand, int>>
                       .Start(moduleVersionPtr, mdToken, opCode, methodName)
                       .WithConcreteType(targetType)
                       .WithNamespaceAndNameFilters(ClrNames.Int32)
                       .Build();
            }
            catch (Exception ex)
            {
                Log.ErrorRetrievingMethod(
                    exception: ex,
                    moduleVersionPointer: moduleVersionPtr,
                    mdToken: mdToken,
                    opCode: opCode,
                    instrumentedType: SystemSqlCommandTypeName,
                    methodName: methodName,
                    instanceType: command.GetType().AssemblyQualifiedName);
                throw;
            }

            var dbCommand = command as DbCommand;

            using (var scope = ScopeFactory.CreateDbCommandScope(Tracer.Instance, dbCommand))
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
        /// Instrumentation wrapper for SqlCommand.ExecuteNonQueryAsync().
        /// </summary>
        /// <param name="command">The object referenced by this in the instrumented method.</param>
        /// <param name="boxedCancellationToken">The <see cref="CancellationToken"/> value used in the original method call.</param>
        /// <param name="opCode">The OpCode used in the original method call.</param>
        /// <param name="mdToken">The mdToken of the original method call.</param>
        /// <param name="moduleVersionPtr">A pointer to the module version GUID.</param>
        /// <returns>The value returned by the instrumented method.</returns>
        [InterceptMethod(
            TargetAssemblies = new[] { AdoNetConstants.AssemblyNames.SystemData, SystemSqlAssemblyName },
            TargetType = SystemSqlCommandTypeName,
            TargetSignatureTypes = new[] { "System.Threading.Tasks.Task`1<System.Int32>", ClrNames.CancellationToken },
            TargetMinimumVersion = Major4,
            TargetMaximumVersion = Major5)]
        public static object ExecuteNonQueryAsync(
            object command,
            object boxedCancellationToken,
            int opCode,
            int mdToken,
            long moduleVersionPtr)
        {
            return ExecuteNonQueryAsyncInternal(
                (DbCommand)command,
                (CancellationToken)boxedCancellationToken,
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
            const string methodName = AdoNetConstants.MethodNames.ExecuteNonQueryAsync;
            Func<DbCommand, CancellationToken, Task<int>> instrumentedMethod;

            try
            {
                var targetType = command.GetInstrumentedType(SystemSqlCommandTypeName);

                instrumentedMethod =
                    MethodBuilder<Func<DbCommand, CancellationToken, Task<int>>>
                       .Start(moduleVersionPtr, mdToken, opCode, methodName)
                       .WithConcreteType(targetType)
                       .WithParameters(cancellationToken)
                       .WithNamespaceAndNameFilters(ClrNames.GenericTask, ClrNames.CancellationToken)
                       .Build();
            }
            catch (Exception ex)
            {
                Log.ErrorRetrievingMethod(
                    exception: ex,
                    moduleVersionPointer: moduleVersionPtr,
                    mdToken: mdToken,
                    opCode: opCode,
                    instrumentedType: SystemSqlCommandTypeName,
                    methodName: methodName,
                    instanceType: command.GetType().AssemblyQualifiedName);
                throw;
            }

            using (var scope = ScopeFactory.CreateDbCommandScope(Tracer.Instance, command))
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
        /// Instrumentation wrapper for SqlCommand.ExecuteScalar().
        /// </summary>
        /// <param name="command">The object referenced by this in the instrumented method.</param>
        /// <param name="opCode">The OpCode used in the original method call.</param>
        /// <param name="mdToken">The mdToken of the original method call.</param>
        /// <param name="moduleVersionPtr">A pointer to the module version GUID.</param>
        /// <returns>The value returned by the instrumented method.</returns>
        [InterceptMethod(
            TargetAssemblies = new[] { AdoNetConstants.AssemblyNames.SystemData, SystemSqlAssemblyName },
            TargetType = SystemSqlCommandTypeName,
            TargetSignatureTypes = new[] { ClrNames.Object },
            TargetMinimumVersion = Major4,
            TargetMaximumVersion = Major5)]
        public static object ExecuteScalar(
            object command,
            int opCode,
            int mdToken,
            long moduleVersionPtr)
        {
            const string methodName = AdoNetConstants.MethodNames.ExecuteScalar;
            Func<DbCommand, object> instrumentedMethod;

            try
            {
                var targetType = command.GetInstrumentedType(SystemSqlCommandTypeName);

                instrumentedMethod =
                    MethodBuilder<Func<DbCommand, object>>
                       .Start(moduleVersionPtr, mdToken, opCode, methodName)
                       .WithConcreteType(targetType)
                       .WithNamespaceAndNameFilters(ClrNames.Object)
                       .Build();
            }
            catch (Exception ex)
            {
                Log.ErrorRetrievingMethod(
                    exception: ex,
                    moduleVersionPointer: moduleVersionPtr,
                    mdToken: mdToken,
                    opCode: opCode,
                    instrumentedType: SystemSqlCommandTypeName,
                    methodName: methodName,
                    instanceType: command.GetType().AssemblyQualifiedName);
                throw;
            }

            var dbCommand = command as DbCommand;

            using (var scope = ScopeFactory.CreateDbCommandScope(Tracer.Instance, dbCommand))
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
        /// Instrumentation wrapper for SqlCommand.ExecuteScalarAsync().
        /// </summary>
        /// <param name="command">The object referenced by this in the instrumented method.</param>
        /// <param name="boxedCancellationToken">The <see cref="CancellationToken"/> value used in the original method call.</param>
        /// <param name="opCode">The OpCode used in the original method call.</param>
        /// <param name="mdToken">The mdToken of the original method call.</param>
        /// <param name="moduleVersionPtr">A pointer to the module version GUID.</param>
        /// <returns>The value returned by the instrumented method.</returns>
        [InterceptMethod(
            TargetAssemblies = new[] { AdoNetConstants.AssemblyNames.SystemData, SystemSqlAssemblyName },
            TargetType = SystemSqlCommandTypeName,
            TargetSignatureTypes = new[] { "System.Threading.Tasks.Task`1<System.Object>", ClrNames.CancellationToken },
            TargetMinimumVersion = Major4,
            TargetMaximumVersion = Major5)]
        public static object ExecuteScalarAsync(
            object command,
            object boxedCancellationToken,
            int opCode,
            int mdToken,
            long moduleVersionPtr)
        {
            return ExecuteScalarAsyncInternal(
                (DbCommand)command,
                (CancellationToken)boxedCancellationToken,
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
            const string methodName = AdoNetConstants.MethodNames.ExecuteScalarAsync;
            Func<DbCommand, CancellationToken, Task<object>> instrumentedMethod;

            try
            {
                var targetType = command.GetInstrumentedType(SystemSqlCommandTypeName);

                instrumentedMethod =
                    MethodBuilder<Func<DbCommand, CancellationToken, Task<object>>>
                       .Start(moduleVersionPtr, mdToken, opCode, methodName)
                       .WithConcreteType(targetType)
                       .WithParameters(cancellationToken)
                       .WithNamespaceAndNameFilters(ClrNames.GenericTask, ClrNames.CancellationToken)
                       .Build();
            }
            catch (Exception ex)
            {
                Log.ErrorRetrievingMethod(
                    exception: ex,
                    moduleVersionPointer: moduleVersionPtr,
                    mdToken: mdToken,
                    opCode: opCode,
                    instrumentedType: SystemSqlCommandTypeName,
                    methodName: methodName,
                    instanceType: command.GetType().AssemblyQualifiedName);
                throw;
            }

            using (var scope = ScopeFactory.CreateDbCommandScope(Tracer.Instance, command))
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
