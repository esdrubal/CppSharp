using System;
using System.Collections.Generic;

namespace CppSharp.AST
{
    /// <summary>
    /// Represents a C/C++ enumeration declaration.
    /// </summary>
    public class Enumeration : Declaration
    {
        [Flags]
        public enum EnumModifiers
        {
            Anonymous,
            Scoped,
            Flags
        }

        /// <summary>
        /// Represents a C/C++ enumeration item.
        /// </summary>
        public class Item : INamedDecl
        {
            public string Name { get; set; }
            public ulong Value;
            public string Expression;
            public string Comment;
            public bool ExplicitValue = true;

            public bool IsHexadecimal
            {
                get
                { 
                    return Expression.Contains("0x") || Expression.Contains("0X");
                }
            }
        }

        public Enumeration()
        {
            Items = new List<Item>();
            ItemsByName = new Dictionary<string, Item>();
            BuiltinType = new BuiltinType(PrimitiveType.Int32);
        }

        public Enumeration AddItem(Item item)
        {
            Items.Add(item);
            ItemsByName[item.Name] = item;
            return this;
        }

        public string GetItemValueAsString(Item item)
        {
            var format = item.IsHexadecimal ? "x" : string.Empty;
            var value = BuiltinType.IsUnsigned ? item.Value.ToString(format) :
                ((long)item.Value).ToString(format);
            return item.IsHexadecimal ? "0x" + value : value;
        }

        public Enumeration SetFlags()
        {
            Modifiers |= EnumModifiers.Flags;
            return this;
        }

        public bool IsFlags
        {
            get { return Modifiers.HasFlag(EnumModifiers.Flags); }
        }

        public Type Type { get; set; }
        public BuiltinType BuiltinType { get; set; }
        public EnumModifiers Modifiers { get; set; }

        public List<Item> Items;
        public Dictionary<string, Item> ItemsByName;

        public override T Visit<T>(IDeclVisitor<T> visitor)
        {
            return visitor.VisitEnumDecl(this);
        }
    }
}