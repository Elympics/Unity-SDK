using System.Linq;
using Mono.Cecil;
using Mono.Collections.Generic;

namespace Elympics.Editor.Weaving.Components
{
    [System.Serializable]
    public class ComponentController
    {
        public int TotalTypesVisited { get; private set; }
        public int TotalMethodsVisited { get; private set; }
        public int TotalFieldsVisited { get; private set; }
        public int TotalPropertiesVisited { get; private set; }

        private DefinitionType _activeDefinitions;
        private WeaverComponent[] _components;

        public ComponentController(params WeaverComponent[] components)
        {
            _components = components;
            foreach (var component in _components)
                _activeDefinitions |= component.AffectedDefinitions;
        }

        public void VisitModule(ModuleDefinition moduleCollection)
        {
            TotalTypesVisited = 0;
            TotalMethodsVisited = 0;
            TotalFieldsVisited = 0;
            TotalPropertiesVisited = 0;

            if (_activeDefinitions == DefinitionType.None)
                return;

            foreach (var component in _components)
                component.OnBeforeModuleEdited(moduleCollection);

            foreach (var component in _components)
                if ((_activeDefinitions & DefinitionType.Module) == DefinitionType.Module)
                    component.VisitModule(moduleCollection);

            VisitTypes(moduleCollection.Types);

            foreach (var component in _components)
                component.OnModuleEditComplete(moduleCollection);
        }

        protected void VisitTypes(Collection<TypeDefinition> typeCollection)
        {
            if ((_activeDefinitions & ~DefinitionType.Module) == DefinitionType.None)
                return;

            foreach (var type in typeCollection.Reverse())
            {
                foreach (var component in _components)
                    component.VisitType(type);

                VisitMethods(type.Methods);
                VisitFields(type.Fields);
                VisitProperties(type.Properties);

                TotalTypesVisited++;
            }
        }

        protected void VisitMethods(Collection<MethodDefinition> methodCollection)
        {
            if ((_activeDefinitions & DefinitionType.Method) != DefinitionType.Method)
                return;

            foreach (var method in methodCollection.Reverse())
            {
                foreach (var component in _components)
                    component.VisitMethod(method);

                TotalMethodsVisited++;
            }
        }

        protected void VisitFields(Collection<FieldDefinition> fieldCollection)
        {
            if ((_activeDefinitions & DefinitionType.Field) != DefinitionType.Field)
                return;

            foreach (var field in fieldCollection.Reverse())
            {
                foreach (var component in _components)
                    component.VisitField(field);

                TotalFieldsVisited++;
            }
        }

        protected void VisitProperties(Collection<PropertyDefinition> propertyCollection)
        {
            if ((_activeDefinitions & DefinitionType.Property) != DefinitionType.Property)
                return;

            foreach (var property in propertyCollection.Reverse())
            {
                foreach (var component in _components)
                    component.VisitProperty(property);

                TotalPropertiesVisited++;
            }
        }
    }
}
