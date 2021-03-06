﻿using System;
using System.Diagnostics;
using System.Linq;
using CppSharp.AST;

namespace CppSharp.Passes
{
    /// <summary>
    /// This pass checks for compatible combinations of getters/setters methods
    /// and creates matching properties that call back into the methods.
    /// </summary>
    public class GetterSetterToPropertyPass : TranslationUnitPass
    {
        public GetterSetterToPropertyPass()
        {
            Options.VisitClassFields = false;
            Options.VisitClassProperties = false;
            Options.VisitNamespaceEnums = false;
            Options.VisitNamespaceTemplates = false;
            Options.VisitNamespaceTypedefs = false;
            Options.VisitNamespaceEvents = false;
            Options.VisitNamespaceVariables = false;
            Options.VisitFunctionParameters = false;
            Options.VisitTemplateArguments = false;
        }

        static bool IsSetter(Function method)
        {
            var isRetVoid = method.ReturnType.Type.IsPrimitiveType(
                PrimitiveType.Void);

            var isSetter = method.OriginalName.StartsWith("set",
                StringComparison.InvariantCultureIgnoreCase);

            return isRetVoid && isSetter && method.Parameters.Count == 1;
        }

        static bool IsGetter(Function method)
        {
            var isRetVoid = method.ReturnType.Type.IsPrimitiveType(
                PrimitiveType.Void);

            var isGetter = method.OriginalName.StartsWith("get",
                StringComparison.InvariantCultureIgnoreCase);

            return !isRetVoid && isGetter && method.Parameters.Count == 0;
        }

        Property GetOrCreateProperty(Class @class, string name, QualifiedType type)
        {
            var prop = @class.Properties.FirstOrDefault(property => property.Name == name
                && property.QualifiedType.Equals(type));

            var prop2 = @class.Properties.FirstOrDefault(property => property.Name == name);

            if (prop == null && prop2 != null)
                Driver.Diagnostics.EmitWarning(DiagnosticId.PropertySynthetized,
                    "Property {0}::{1} already exist with type {2}", @class.Name, name, type.Type.ToString());

            if (prop != null)
                return prop;

            prop = new Property
            {
                Name = name,
                Namespace = @class,
                QualifiedType = type
            };

            @class.Properties.Add(prop);
            return prop;
        }

        public override bool VisitMethodDecl(Method method)
        {
            if (AlreadyVisited(method))
                return false;

            if (ASTUtils.CheckIgnoreMethod(method))
                return false;

            var @class = method.Namespace as Class;

            if (@class == null || @class.IsIncomplete)
                return false;

            if (IsGetter(method))
            {
                var name = method.Name.Substring("get".Length);
                var prop = GetOrCreateProperty(@class, name, method.ReturnType);
                prop.GetMethod = method;

                // Do not generate the original method now that we know it is a getter.
                method.IsGenerated = false;

                Driver.Diagnostics.EmitMessage(DiagnosticId.PropertySynthetized,
                    "Getter created: {0}::{1}", @class.Name, name);

                return false;
            }

            if (IsSetter(method) && IsValidSetter(method))
            {
                var name = method.Name.Substring("set".Length);

                var type = method.Parameters[0].QualifiedType;
                var prop = GetOrCreateProperty(@class, name, type);
                prop.SetMethod = method;

                // Ignore the original method now that we know it is a setter.
                method.IsGenerated = false;

                Driver.Diagnostics.EmitMessage(DiagnosticId.PropertySynthetized,
                    "Setter created: {0}::{1}", @class.Name, name);

                return false;
            }

            return false;
        }

        // Check if a matching getter exist or no other setter exists.
        private bool IsValidSetter(Method method)
        {
            var @class = method.Namespace as Class;
            var name = method.Name.Substring("set".Length);

            if (method.Parameters.Count == 0)
                return false;

            var type = method.Parameters[0].Type;

            var getter = @class.Methods.FirstOrDefault(m => m.Name == "Get" + name && m.Type.Equals(type));

            var otherSetter = @class.Methods.FirstOrDefault(m => m.Name == method.Name
                && m.Parameters.Count == 1 
                && !m.Parameters[0].Type.Equals(type));

            return getter != null || otherSetter == null;
        }
    }
}
