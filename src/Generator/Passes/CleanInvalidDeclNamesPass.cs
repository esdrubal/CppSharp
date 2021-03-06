﻿using System;
using System.Linq;
using CppSharp.AST;

namespace CppSharp.Passes
{
    public class CleanInvalidDeclNamesPass : TranslationUnitPass
    {
        private int uniqueName;

        public CleanInvalidDeclNamesPass()
        {
        }

        string CheckName(string name)
        {
            // Generate a new name if the decl still does not have a name
            if (string.IsNullOrWhiteSpace(name))
                return string.Format("_{0}", uniqueName++);

            var firstChar = name.FirstOrDefault();

            // Clean up the item name if the first digit is not a valid name.
            if (char.IsNumber(firstChar))
                return '_' + name;

            return name;
        }

        public override bool VisitDeclaration(Declaration decl)
        {
            // Do not clean up namespace names since it can mess up with the
            // names of anonymous or the global namespace.
            if (decl is Namespace)
                return true;

            decl.Name = CheckName(decl.Name);

            StringHelpers.CleanupText(ref decl.DebugText);
            return base.VisitDeclaration(decl);
        }

        public override bool VisitFunctionDecl(Function function)
        {
            uniqueName = 0;
            return base.VisitFunctionDecl(function);
        }

        public override bool VisitTypedefDecl(TypedefDecl typedef)
        {
            var @class = typedef.Namespace.FindClass(typedef.Name);

            // Clang will walk the typedef'd tag decl and the typedef decl,
            // so we ignore the class and process just the typedef.

            if (@class != null)
                typedef.ExplicityIgnored = true;

            if (typedef.Type == null)
                typedef.ExplicityIgnored = true;

            return base.VisitTypedefDecl(typedef);
        }

        private static void CheckEnumName(Enumeration @enum)
        {
            // If we still do not have a valid name, then try to guess one
            // based on the enum value names.

            if (!String.IsNullOrWhiteSpace(@enum.Name))
                return;

            var prefix = @enum.Items.Select(item => item.Name)
                .ToArray().CommonPrefix();

            // Try a simple heuristic to make sure we end up with a valid name.
            if (prefix.Length < 3)
                return;

            prefix = prefix.Trim().Trim(new char[] { '_' });
            @enum.Name = prefix;
        }

        public override bool VisitEnumDecl(Enumeration @enum)
        {
            CheckEnumName(@enum);
            return base.VisitEnumDecl(@enum);
        }

        public override bool VisitEnumItem(Enumeration.Item item)
        {
            item.Name = CheckName(item.Name);
            return base.VisitEnumItem(item);
        }
    }

    public static class CleanInvalidDeclNamesExtensions
    {
        public static void CleanInvalidDeclNames(this PassBuilder builder)
        {
            var pass = new CleanInvalidDeclNamesPass();
            builder.AddPass(pass);
        }
    }
}

