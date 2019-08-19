using System;
using System.Reflection;
using Datadog.Trace.ClrProfiler.Emit;
using Xunit;

namespace Datadog.Trace.ClrProfiler.Managed.Tests
{
    /// <summary>
    /// All delegates we generate for instance methods include the instance as the first parameter
    /// </summary>
    public class MethodBuilderTests
    {
        private readonly Assembly _thisAssembly = Assembly.GetExecutingAssembly();
        private readonly Type _testType = typeof(ObscenelyAnnoyingClass);

        [Fact]
        public void NoParameters_ProperlyCalled()
        {
            var instance = new ObscenelyAnnoyingClass();
            var expected = MethodReference.Get(() => instance.Method());
            var methodResult = Build<Action<object>>(expected.Name).Build();
            methodResult.Invoke(instance);
            Assert.Equal(expected: expected.MetadataToken, instance.LastCall.MetadataToken);
        }

        [Fact]
        public void IntParameter_ProperlyCalled()
        {
            var instance = new ObscenelyAnnoyingClass();
            int parameter = 1;
            var expected = MethodReference.Get(() => instance.Method(parameter));
            var methodResult = Build<Action<object, int>>(expected.Name).WithParameters(parameter).Build();
            methodResult.Invoke(instance, parameter);
            Assert.Equal(expected: expected.MetadataToken, instance.LastCall.MetadataToken);
        }

        [Fact]
        public void LongParameter_ProperlyCalled()
        {
            var instance = new ObscenelyAnnoyingClass();
            long parameter = 1;
            var expected = MethodReference.Get(() => instance.Method(parameter));
            var methodResult = Build<Action<object, long>>(expected.Name).WithParameters(parameter).Build();
            methodResult.Invoke(instance, parameter);
            Assert.Equal(expected: expected.MetadataToken, instance.LastCall.MetadataToken);
        }

        [Fact]
        public void ShortParameter_ProperlyCalled()
        {
            var instance = new ObscenelyAnnoyingClass();
            short parameter = 1;
            var expected = MethodReference.Get(() => instance.Method(parameter));
            var methodResult = Build<Action<object, short>>(expected.Name).WithParameters(parameter).Build();
            methodResult.Invoke(instance, parameter);
            Assert.Equal(expected: expected.MetadataToken, instance.LastCall.MetadataToken);
        }

        [Fact]
        public void ObjectParameter_ProperlyCalled()
        {
            var instance = new ObscenelyAnnoyingClass();
            object parameter = new object();
            var expected = MethodReference.Get(() => instance.Method(parameter));
            var methodResult = Build<Action<object, object>>(expected.Name).WithParameters(parameter).Build();
            methodResult.Invoke(instance, parameter);
            Assert.Equal(expected: expected.MetadataToken, instance.LastCall.MetadataToken);
        }

        [Fact]
        public void StringParameter_ProperlyCalled()
        {
            var instance = new ObscenelyAnnoyingClass();
            string parameter = string.Empty;
            var expected = MethodReference.Get(() => instance.Method(parameter));
            var methodResult = Build<Action<object, string>>(expected.Name).WithParameters(parameter).Build();
            methodResult.Invoke(instance, parameter);
            Assert.Equal(expected: expected.MetadataToken, instance.LastCall.MetadataToken);
        }

        [Fact]
        public void StringParameterAsObject_ProperlyCalls_ObjectMethod()
        {
            var instance = new ObscenelyAnnoyingClass();
            object parameter = string.Empty;
            var expected = MethodReference.Get(() => instance.Method(parameter));
            var methodResult = Build<Action<object, object>>(expected.Name).WithParameters(parameter).Build();
            methodResult.Invoke(instance, parameter);
            Assert.Equal(expected: expected.MetadataToken, instance.LastCall.MetadataToken);
        }

        [Fact]
        public void StringParameterAsObject_WithExplicitTypeSpecified_ProperlyCalls_StringMethod()
        {
            var instance = new ObscenelyAnnoyingClass();
            object parameter = string.Empty;
            var expected = MethodReference.Get(() => instance.Method(string.Empty));
            var methodResult =
                Build<Action<object, object>>(expected.Name)
                   .WithParameters(parameter)
                   .WithExplicitParameterTypes(typeof(string))
                   .Build();
            methodResult.Invoke(instance, parameter);
            Assert.Equal(expected: expected.MetadataToken, instance.LastCall.MetadataToken);
        }

        [Fact]
        public void DeclaringTypeGenericParameter_ProperlyCalls_ClosedGenericMethod()
        {
            var instance = new ObscenelyAnnoyingGenericClass<ClassA>();
            var parameter = new ClassA();
            var expected = MethodReference.Get(() => instance.Method(parameter));
            var methodResult = Build<Action<object, object>>(expected.Name, overrideType: instance.GetType()).WithParameters(parameter).Build();
            methodResult.Invoke(instance, parameter);
            Assert.Equal(expected: expected.MetadataToken, instance.LastCall.MetadataToken);
        }

        [Fact]
        public void DeclaringTypeGenericParameter_WithOpenGenericMethod_ProperlyCalls_OpenGenericMethod()
        {
            var instance = new ObscenelyAnnoyingGenericClass<ClassA>();
            var parameter = new ClassA();
            var expected = MethodReference.Get(() => instance.Method<int>(parameter));
            var methodResult =
                Build<Action<object, object>>(expected.Name, overrideType: instance.GetType())
                   .WithParameters(parameter)
                   .WithMethodGenerics(typeof(int))
                   .Build();
            methodResult.Invoke(instance, parameter);
            Assert.Equal(expected: expected.MetadataToken, instance.LastCall.MetadataToken);
        }

        [Fact]
        public void DeclaringTypeGenericTypeParam_ThenMethodGenericParam_ProperlyCalls_Method()
        {
            var instance = new ObscenelyAnnoyingGenericClass<ClassA>();
            var parameter1 = new ClassA();
            int parameter2 = 1;
            var expected = MethodReference.Get(() => instance.Method<int>(parameter1, parameter2));
            var methodResult =
                Build<Action<object, object, int>>(expected.Name, overrideType: instance.GetType())
                   .WithParameters(parameter1, parameter2)
                   .WithMethodGenerics(typeof(int))
                   .Build();
            methodResult.Invoke(instance, parameter1, parameter2);
            Assert.Equal(expected: expected.MetadataToken, instance.LastCall.MetadataToken);
        }

        [Fact]
        public void WrongMetadataToken_NonSpecificDelegateSignature_GetsCorrectMethodAnyways()
        {
            var instance = new ObscenelyAnnoyingClass();
            var wrongMethod = MethodReference.Get(() => instance.Method(1));

            string parameter = string.Empty;
            var expected = MethodReference.Get(() => instance.Method(parameter));

            var builder = MethodBuilder<Action<object, object>> // Proper use should be Action<object, string>
                              .Start(_thisAssembly, wrongMethod.MetadataToken, (int)OpCodeValue.Callvirt, "Method")
                              .WithConcreteType(_testType)
                              .WithParameters(parameter); // The parameter is the saving grace

            // We are intentionally testing fallbacks, so we don't want this exception
            builder.ThrowExceptionWhenNoTokenMatch = false;

            var methodResult = builder.Build();

            methodResult.Invoke(instance, parameter);
            Assert.Equal(expected: expected.MetadataToken, instance.LastCall.MetadataToken);
        }

        private MethodBuilder<T> Build<T>(string methodName, Type overrideType = null)
        {
            var builder = MethodBuilder<T>
                  .Start(_thisAssembly, 0, (int)OpCodeValue.Callvirt, methodName)
                  .WithConcreteType(overrideType ?? _testType);

            // We are intentionally testing fallbacks, so we don't want this exception
            builder.ThrowExceptionWhenNoTokenMatch = false;

            return builder;
        }
    }
}
