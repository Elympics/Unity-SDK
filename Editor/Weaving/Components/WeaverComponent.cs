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
        private ModuleDefinition _activeModule;

        protected TypeSystem TypeSystem => _activeModule?.TypeSystem;

        public virtual DefinitionType AffectedDefinitions => DefinitionType.None;

        public void OnBeforeModuleEdited(ModuleDefinition moduleDefinition)
        {
            _activeModule = moduleDefinition;
            StartVisiting(moduleDefinition);
        }

        public void OnModuleEditComplete(ModuleDefinition moduleDefinition)
        {
            FinishVisiting(moduleDefinition);
            _activeModule = null;
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
