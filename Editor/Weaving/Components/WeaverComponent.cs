#nullable enable
using System;
using Mono.Cecil;

namespace Elympics.Editor.Weaving.Components
{
    [Flags]
    internal enum DefinitionType
    {
        None = 0,
        Module = 1 << 1,
        Type = 1 << 2,
        Method = 1 << 3,
        Field = 1 << 4,
        Property = 1 << 5,
        All = Module | Type | Method | Field | Property
    }

    internal abstract class WeaverComponent
    {
        protected ModuleDefinition? Module { get; private set; }
        protected AssemblyDefinition? Assembly => Module?.Assembly;
        protected TypeSystem? TypeSystem => Module?.TypeSystem;

        public virtual DefinitionType AffectedDefinitions => DefinitionType.None;

        public void OnBeforeModuleEdited(ModuleDefinition moduleDefinition)
        {
            Module = moduleDefinition;
            StartVisiting(moduleDefinition);
        }

        public void OnModuleEditComplete(ModuleDefinition moduleDefinition)
        {
            FinishVisiting(moduleDefinition);
            Module = null;
        }

        protected virtual void StartVisiting(ModuleDefinition moduleDefinition) { }
        public virtual void VisitModule(ModuleDefinition moduleDefinition) { }
        public virtual void VisitType(TypeDefinition typeDefinition) { }
        public virtual void VisitMethod(MethodDefinition methodDefinition) { }
        public virtual void VisitField(FieldDefinition fieldDefinition) { }
        public virtual void VisitProperty(PropertyDefinition propertyDefinition) { }
        protected virtual void FinishVisiting(ModuleDefinition moduleDefinition) { }
    }
}
