﻿using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using Csla.Properties;
using System.Collections.Generic;

namespace Csla.Reflection
{
  /// <summary>
  /// Delegate for a dynamic constructor method.
  /// </summary>
  public delegate object DynamicCtorDelegate();
  /// <summary>
  /// Delegate for a dynamic method.
  /// </summary>
  /// <param name="target">
  /// Object containg method to invoke.
  /// </param>
  /// <param name="args">
  /// Parameters passed to method.
  /// </param>
  public delegate object DynamicMethodDelegate(object target, object[] args);
  /// <summary>
  /// Delegate for getting a value.
  /// </summary>
  /// <param name="target">Target object.</param>
  /// <returns></returns>
  public delegate object DynamicMemberGetDelegate(object target);
  /// <summary>
  /// Delegate for setting a value.
  /// </summary>
  /// <param name="target">Target object.</param>
  /// <param name="arg">Argument value.</param>
  public delegate void DynamicMemberSetDelegate(object target, object arg);

  internal static class DynamicMethodHandlerFactory
  {
    public static DynamicCtorDelegate CreateConstructor(ConstructorInfo constructor)
    {
      if (constructor == null)
        throw new ArgumentNullException("constructor");
      if (constructor.GetParameters().Length > 0)
        throw new NotSupportedException(Resources.ConstructorsWithParametersNotSupported);

      Expression body = Expression.New(constructor);
      if (constructor.DeclaringType.IsValueType)
      {
        body = Expression.Convert(body, typeof(object));
      }

      return Expression.Lambda<DynamicCtorDelegate>(body).Compile();
    }

    public static DynamicMethodDelegate CreateMethod(MethodInfo method)
    {
      if (method == null)
        throw new ArgumentNullException("method");

      ParameterInfo[] pi = method.GetParameters();
      var targetExpression = Expression.Parameter(typeof(object));
      var parametersExpression = Expression.Parameter(typeof(object[]));

      Expression[] callParametrs = new Expression[pi.Length];
      for (int x = 0; x < pi.Length; x++)
      {
        callParametrs[x] =
          Expression.Convert(
            Expression.ArrayIndex(
              parametersExpression,
              Expression.Constant(x)),
            pi[x].ParameterType);
      }

      Expression instance = Expression.Convert(targetExpression, method.DeclaringType);
      Expression body = pi.Length > 0
        ? Expression.Call(instance, method, callParametrs)
        : Expression.Call(instance, method);

      if (method.ReturnType == typeof(void))
      {
        var target = Expression.Label(typeof(object));
        var nullRef = Expression.Constant(null);
        body = Expression.Block(
          body,
           Expression.Return(target, nullRef),
          Expression.Label(target, nullRef));
      }
      else if (method.ReturnType.IsValueType)
      {
        body = Expression.Convert(body, typeof(object));
      }

      var lambda = Expression.Lambda<DynamicMethodDelegate>(
        body,
        targetExpression,
        parametersExpression);

      return (DynamicMethodDelegate)lambda.Compile();
    }

    public static DynamicMemberGetDelegate CreatePropertyGetter(PropertyInfo property)
    {
      if (property == null)
        throw new ArgumentNullException("property");

      if (!property.CanRead) return null;

      var target = Expression.Parameter(typeof(object));
      Expression body = Expression.Property(
        Expression.Convert(target, property.DeclaringType),
        property);

      if (property.PropertyType.IsValueType)
      {
        body = Expression.Convert(body, typeof(object));
      }

      var lambda = Expression.Lambda<DynamicMemberGetDelegate>(
        body,
        target);

      return lambda.Compile();
    }

    public static DynamicMemberSetDelegate CreatePropertySetter(PropertyInfo property)
    {
      if (property == null)
        throw new ArgumentNullException("property");

      if (!property.CanWrite) return null;

      var target = Expression.Parameter(typeof(object));
      var val = Expression.Parameter(typeof(object));

      Expression body = Expression.Assign(
        Expression.Property(
          Expression.Convert(target, property.DeclaringType),
          property),
        Expression.Convert(val, property.PropertyType));

      var lambda = Expression.Lambda<DynamicMemberSetDelegate>(
        body,
        target,
        val);

      return lambda.Compile();
    }

    public static DynamicMemberGetDelegate CreateFieldGetter(FieldInfo field)
    {
      if (field == null)
        throw new ArgumentNullException("field");

      DynamicMethod dm = new DynamicMethod("fldg", typeof(object),
          new Type[] { typeof(object) },
          field.DeclaringType, true);

      ILGenerator il = dm.GetILGenerator();

      if (!field.IsStatic)
      {
        il.Emit(OpCodes.Ldarg_0);

        EmitCastToReference(il, field.DeclaringType);  //to handle struct object

        il.Emit(OpCodes.Ldfld, field);
      }
      else
        il.Emit(OpCodes.Ldsfld, field);

      if (field.FieldType.IsValueType)
        il.Emit(OpCodes.Box, field.FieldType);

      il.Emit(OpCodes.Ret);

      return (DynamicMemberGetDelegate)dm.CreateDelegate(typeof(DynamicMemberGetDelegate));
    }

    public static DynamicMemberSetDelegate CreateFieldSetter(FieldInfo field)
    {
      if (field == null)
        throw new ArgumentNullException("field");

      DynamicMethod dm = new DynamicMethod("flds", null,
          new Type[] { typeof(object), typeof(object) },
          field.DeclaringType, true);

      ILGenerator il = dm.GetILGenerator();

      if (!field.IsStatic)
      {
        il.Emit(OpCodes.Ldarg_0);
      }
      il.Emit(OpCodes.Ldarg_1);

      EmitCastToReference(il, field.FieldType);

      if (!field.IsStatic)
        il.Emit(OpCodes.Stfld, field);
      else
        il.Emit(OpCodes.Stsfld, field);
      il.Emit(OpCodes.Ret);

      return (DynamicMemberSetDelegate)dm.CreateDelegate(typeof(DynamicMemberSetDelegate));
    }

    private static void EmitCastToReference(ILGenerator il, Type type)
    {
      if (type.IsValueType)
        il.Emit(OpCodes.Unbox_Any, type);
      else
        il.Emit(OpCodes.Castclass, type);
    }

  }
}