﻿using System.Collections.Generic;

namespace Cxxi
{
    /// <summary>
    /// Represents a C++ type reference.
    /// </summary>
    public abstract class Type
    {
        public static ITypeVisitor<string> TypePrinter;

        protected Type()
        {
        }

        public bool IsPrimitiveType(PrimitiveType primitive)
        {
            var builtin = this as BuiltinType;
            if (builtin != null)
                return builtin.Type == primitive;
            return false;
        }

        public bool IsEnumType()
        {
            var tag = this as TagType;
            
            if (tag == null)
                return false;

            return tag.Declaration is Enumeration;
        }

        public bool IsPointerToPrimitiveType(PrimitiveType primitive)
        {
            var ptr = this as PointerType;
            if (ptr == null)
                return false;
            return ptr.Pointee.IsPrimitiveType(primitive);
        }

        public bool IsPointerTo<T>(out T type) where T : Type
        {
            var ptr = this as PointerType;
            
            if (ptr == null)
            {
                type = null;
                return false;
            }
            
            type = ptr.Pointee as T;
            return type != null;
        }

        public bool IsTagDecl<T>(out T decl) where T : Declaration
        {
            var tag = this as TagType;
            
            if (tag == null)
            {
                decl = null;
                return false;
            }

            decl = tag.Declaration as T;
            return decl != null;
        }

        public abstract T Visit<T>(ITypeVisitor<T> visitor, TypeQualifiers quals
            = new TypeQualifiers());

        public override string ToString()
        {
            return Visit(TypePrinter);
        }
    }

    public struct TypeQualifiers
    {
        public bool IsConst;
        public bool IsVolatile;
        public bool IsRestrict;
    }

    /// <summary>
    /// Represents a C++ tag type reference.
    /// </summary>
    public class TagType : Type
    {
        public TagType()
        {
        }

        public Declaration Declaration;

        public override T Visit<T>(ITypeVisitor<T> visitor, TypeQualifiers quals)
        {
            return visitor.VisitTagType(this, quals);
        }
    }

    /// <summary>
    /// Represents an C/C++ array type.
    /// </summary>
    public class ArrayType : Type
    {
        public enum ArraySize
        {
            Constant,
            Variable
        }

        public ArrayType()
        {
        }

        // Type of the array elements.
        public Type Type;

        // Size type of array.
        public ArraySize SizeType;

        // In case of a constant size array.
        public long Size;

        public override T Visit<T>(ITypeVisitor<T> visitor, TypeQualifiers quals)
        {
            return visitor.VisitArrayType(this, quals);
        }
    }

    /// <summary>
    /// Represents an C/C++ function type.
    /// </summary>
    public class FunctionType : Type
    {
        // Return type of the function.
        public Type ReturnType;

        // Argument types.
        public List<Parameter> Arguments;

        public FunctionType()
        {
            Arguments = new List<Parameter>();
        }

        public override T Visit<T>(ITypeVisitor<T> visitor, TypeQualifiers quals)
        {
            return visitor.VisitFunctionType(this, quals);
        }
    }

    /// <summary>
    /// Represents a C++ pointer/reference type.
    /// </summary>
    public class PointerType : Type
    {
        public PointerType()
        {
        
        }

        /// <summary>
        /// Represents the modifiers on a C++ type reference.
        /// </summary>
        public enum TypeModifier
        {
            Value,
            Pointer,
            // L-value references
            LVReference,
            // R-value references
            RVReference
        }

        static string ConvertModifierToString(TypeModifier modifier)
        {
            switch (modifier)
            {
                case TypeModifier.Value: return string.Empty;
                case TypeModifier.Pointer:
                case TypeModifier.LVReference:
                case TypeModifier.RVReference: return "*";
            }

            return string.Empty;
        }

        public bool IsReference
        {
            get
            {
                return Modifier == TypeModifier.LVReference
                       || Modifier == TypeModifier.RVReference;
            }
        }

        public Type Pointee;

        public TypeModifier Modifier;

        public override T Visit<T>(ITypeVisitor<T> visitor, TypeQualifiers quals)
        {
            return visitor.VisitPointerType(this, quals);
        }
    }

    /// <summary>
    /// Represents a C++ member function pointer type.
    /// </summary>
    public class MemberPointerType : Type
    {
        public MemberPointerType()
        {

        }

        public Type Pointee;

        public override T Visit<T>(ITypeVisitor<T> visitor, TypeQualifiers quals)
        {
            return visitor.VisitMemberPointerType(this, quals);
        }
    }

    /// <summary>
    /// Represents a C/C++ typedef type.
    /// </summary>
    public class TypedefType : Type
    {
        public TypedefType()
        {

        }

        public TypedefDecl Declaration;

        public override T Visit<T>(ITypeVisitor<T> visitor, TypeQualifiers quals)
        {
            return visitor.VisitTypedefType(this, quals);
        }
    }

    /// <summary>
    /// Represents a template argument within a class template specialization.
    /// </summary>
    public struct TemplateArgument
    {
        /// The kind of template argument we're storing.
        public enum ArgumentKind
        {
            /// The template argument is a type.
            Type,

            /// The template argument is a declaration that was provided for a
            /// pointer. reference, or pointer to member non-type template
            /// parameter.
            Declaration,

            /// The template argument is a null pointer or null pointer to member
            /// that was provided for a non-type template parameter.
            NullPtr,

            /// The template argument is an integral value that was provided for
            /// an integral non-type template parameter.
            Integral,

            /// The template argument is a template name that was provided for a
            /// template template parameter.
            Template,

            /// The template argument is a pack expansion of a template name that
            /// was provided for a template template parameter.
            TemplateExpansion,

            /// The template argument is a value- or type-dependent expression.
            Expression,

            /// The template argument is actually a parameter pack.
            Pack
        }

        public ArgumentKind Kind;
        public Type Type;
        public Declaration Declaration;
        public long Integral;
    }

    /// <summary>
    /// Represents a C++ template specialization type.
    /// </summary>
    public class TemplateSpecializationType : Type
    {
        public TemplateSpecializationType()
        {
            Arguments = new List<TemplateArgument>();
        }

        public List<TemplateArgument> Arguments;

        public Template Template;

        public override T Visit<T>(ITypeVisitor<T> visitor,
                                   TypeQualifiers quals = new TypeQualifiers())
        {
            return visitor.VisitTemplateSpecializationType(this, quals);
        }
    }

    /// <summary>
    /// Represents a C++ template parameter type.
    /// </summary>
    public class TemplateParameterType : Type
    {
        public TemplateParameterType()
        {
        }

        public TemplateParameter Parameter;
        public Template Template;

        public override T Visit<T>(ITypeVisitor<T> visitor,
                                   TypeQualifiers quals = new TypeQualifiers())
        {
            //return visitor.VisitTemplateParameterType(this, quals);
            return default(T);
        }
    }

    #region Primitives

    /// <summary>
    /// Represents the C++ built-in types.
    /// </summary>
    public enum PrimitiveType
    {
        Null,
        Void,
        Bool,
        WideChar,
        Int8,
        Char = Int8,
        UInt8,
        UChar = UInt8,
        Int16,
        UInt16,
        Int32,
        UInt32,
        Int64,
        UInt64,
        Float,
        Double
    }

    /// <summary>
    /// Represents an instance of a C++ built-in type.
    /// </summary>
    public class BuiltinType : Type
    {
        public BuiltinType()
        {
        }

        public BuiltinType(PrimitiveType type)
        {
            Type = type;
        }

        // Primitive type of built-in type.
        public PrimitiveType Type;

        public override T Visit<T>(ITypeVisitor<T> visitor, TypeQualifiers quals)
        {
            return visitor.VisitBuiltinType(this, quals);
        }
    }

    #endregion

    public interface ITypeVisitor<out T>
    {
        T VisitTagType(TagType tag, TypeQualifiers quals);
        T VisitArrayType(ArrayType array, TypeQualifiers quals);
        T VisitFunctionType(FunctionType function, TypeQualifiers quals);
        T VisitPointerType(PointerType pointer, TypeQualifiers quals);
        T VisitMemberPointerType(MemberPointerType member, TypeQualifiers quals);
        T VisitBuiltinType(BuiltinType builtin, TypeQualifiers quals);
        T VisitTypedefType(TypedefType typedef, TypeQualifiers quals);
        T VisitTemplateSpecializationType(TemplateSpecializationType template,
                                          TypeQualifiers quals);
        T VisitDeclaration(Declaration decl, TypeQualifiers quals);
    }
}