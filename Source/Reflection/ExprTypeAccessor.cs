﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace BLToolkit.Reflection
{
	using TypeBuilder;

	class ExprTypeAccessor<T,TOriginal> : TypeAccessor
	{
		static ExprTypeAccessor()
		{
			// Create Instance.
			//
			var type     = typeof(T);
			var typeInit = typeof(InitContext);
			var initPar  = Expression.Parameter(typeInit, "ctx");

			if (type.IsValueType)
			{
				_createInstance = () => default(T);
				_createInstanceInit = ctx => default(T);
			}
			else
			{
				var ctor = type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, Type.EmptyTypes, null);
				var ctorInit = type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, new[] { typeInit }, null);

				if (ctor == null && ctorInit == null)
				{
					Expression<Func<T>> mi = () => ThrowException();

					var body = Expression.Call(null, ((MethodCallExpression)mi.Body).Method);

					_createInstance = Expression.Lambda<Func<T>>(body).Compile();
					_createInstanceInit = Expression.Lambda<Func<InitContext, T>>(body, initPar).Compile();
				}
				else
				{
					_createInstance = ctor != null ?
						Expression.Lambda<Func<T>>(Expression.New(ctor)).Compile() :
						Expression.Lambda<Func<T>>(Expression.New(ctorInit, Expression.Constant(null))).Compile();

					_createInstanceInit = ctorInit != null ?
						Expression.Lambda<Func<InitContext, T>>(Expression.New(ctorInit, initPar), initPar).Compile() :
						Expression.Lambda<Func<InitContext, T>>(Expression.New(ctor), initPar).Compile();
				}
			}

			var originalType = typeof(TOriginal);

			// Add fields.
			//
			foreach (var fi in originalType.GetFields(BindingFlags.Instance | BindingFlags.Public))
				_members.Add(fi);

			foreach (var pi in originalType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
				if (pi.GetIndexParameters().Length == 0)
					_members.Add(pi);

			// Add implicit iterface implementation properties support
			// Or maybe we should support all private fields/properties?
			var interfaceMethods = originalType.GetInterfaces().SelectMany(ti => originalType.GetInterfaceMap(ti).TargetMethods).ToList();

			if (interfaceMethods.Count > 0)
			{
				foreach (var pi in originalType.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic))
				{
					if (pi.GetIndexParameters().Length == 0)
					{
						var getMethod = pi.GetGetMethod(true);
						var setMethod = pi.GetSetMethod(true);

						if ((getMethod == null || interfaceMethods.Contains(getMethod)) &&
							(setMethod == null || interfaceMethods.Contains(setMethod)))
						{
							_members.Add(pi);
						}
					}
				}
			}

			// ObjectFactory
			//
			var attr = TypeHelper.GetFirstAttribute(type, typeof(ObjectFactoryAttribute));

			if (attr != null)
				_objectFactory = ((ObjectFactoryAttribute)attr).ObjectFactory;
		}

		static T ThrowException()
		{
			throw new TypeBuilderException(string.Format("The '{0}' type must have default or init constructor.", typeof(TOriginal).FullName));
		}

		static readonly List<MemberInfo> _members = new List<MemberInfo>();
		static readonly IObjectFactory   _objectFactory;

		public ExprTypeAccessor()
		{
			foreach (var member in _members)
				AddMember(ExprMemberAccessor.GetMemberAccessor(this, member.Name));

			ObjectFactory = _objectFactory;
		}

		static readonly Func<T> _createInstance;
		public override object   CreateInstance()
		{
			return _createInstance();
		}

		static readonly Func<InitContext,T> _createInstanceInit;

		public override object CreateInstance(InitContext context)
		{
			return _createInstanceInit(context);
		}

		public override Type Type         { get { return typeof(T);         } }
		public override Type OriginalType { get { return typeof(TOriginal); } }
	}
}
