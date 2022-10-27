﻿using ILRuntime.CLR.Method;
using ILRuntime.CLR.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ILRuntime.Reflection
{
    public class ILRuntimeMethodInfo : MethodInfo
    {
        ILMethod method;
        ILRuntimeParameterInfo[] parameters;
        Mono.Cecil.MethodDefinition definition;
        ILRuntime.Runtime.Enviorment.AppDomain appdomain;

        Attribute[] customAttributes;
        Type[] attributeTypes;
        public ILRuntimeMethodInfo(ILMethod m)
        {
            method = m;
            definition = m.Definition;
            appdomain = m.DeclearingType.AppDomain;
            parameters = new ILRuntimeParameterInfo[m.ParameterCount];
            for (int i = 0; i < m.ParameterCount; i++)
            {
                Mono.Cecil.ParameterDefinition pd = m.Definition.Parameters[i];
                parameters[i] = new ILRuntimeParameterInfo(pd, m.Parameters[i], this);
            }
        }

        void InitializeCustomAttribute()
        {
            customAttributes = new Attribute[definition.CustomAttributes.Count];
            attributeTypes = new Type[customAttributes.Length];
            for (int i = 0; i < definition.CustomAttributes.Count; i++)
            {
                Mono.Cecil.CustomAttribute attribute = definition.CustomAttributes[i];
                CLR.TypeSystem.IType at = appdomain.GetType(attribute.AttributeType, null, null);
                try
                {
                    Attribute ins = attribute.CreateInstance(at, appdomain) as Attribute;

                    attributeTypes[i] = at.ReflectionType;
                    customAttributes[i] = ins;
                }
                catch
                {
                    attributeTypes[i] = typeof(Attribute);
                }
            }
        }

        internal ILMethod ILMethod { get { return method; } }
        public override MethodAttributes Attributes
        {
            get
            {
                MethodAttributes ma = MethodAttributes.Public;
                if (method.IsStatic)
                    ma |= MethodAttributes.Static;
                return ma;
            }
        }

        public override Type DeclaringType
        {
            get
            {
                return method.DeclearingType.ReflectionType;
            }
        }

        public override RuntimeMethodHandle MethodHandle
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override string Name
        {
            get
            {
                return method.Name;
            }
        }

        public override Type ReflectedType
        {
            get
            {
                return method.DeclearingType.ReflectionType;
            }
        }

        public override ICustomAttributeProvider ReturnTypeCustomAttributes
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override MethodInfo GetBaseDefinition()
        {
            return this;
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            if (customAttributes == null)
                InitializeCustomAttribute();

            return customAttributes;
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            if (customAttributes == null)
                InitializeCustomAttribute();
            List<object> res = new List<object>();
            for (int i = 0; i < customAttributes.Length; i++)
            {
                if (attributeTypes[i].IsSubclassOf(attributeType) | attributeTypes[i].Equals(attributeType))
                    res.Add(customAttributes[i]);
            }
            return res.ToArray();
        }

        public override MethodImplAttributes GetMethodImplementationFlags()
        {
            throw new NotImplementedException();
        }

        public override ParameterInfo[] GetParameters()
        {
            return parameters;
        }

        public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
        {
            if (method.HasThis)
            {
                object res = appdomain.Invoke(method, obj, parameters);
                return ReturnType.CheckCLRTypes(res);
            }
            else
                return appdomain.Invoke(method, null, parameters);
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            if (customAttributes == null)
                InitializeCustomAttribute();
            for (int i = 0; i < customAttributes.Length; i++)
            {
                if (attributeTypes[i] == attributeType)
                    return true;
            }
            return false;
        }

        public override Type ReturnType
        {
            get
            {
                return method.ReturnType?.ReflectionType;
            }
        }

        public override string ToString()
        {
            return method.ToString();
        }
    }
}