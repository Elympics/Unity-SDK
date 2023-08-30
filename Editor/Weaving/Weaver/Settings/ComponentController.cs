using Mono.Cecil;
using Mono.Collections.Generic;

namespace Elympics.Weaver
{
    [System.Serializable]
    public class ComponentController
    {
        /// <summary>
        /// This is used to only loop over definition types that
        /// we are using.
        /// </summary>
        private DefinitionType m_ActiveDefinitions;
        private WeaverComponent[] m_SubObjects;

        public int totalTypesVisited { get; private set; }
        public int totalMethodsVisited { get; private set; }
        public int totalFieldsVisited { get; private set; }
        public int totalPropertiesVisited { get; private set; }

        public ComponentController(params WeaverComponent[] components)
        {
            m_SubObjects = components;
            foreach (var component in m_SubObjects)
                m_ActiveDefinitions |= component.EffectedDefintions;
        }

        /// <summary>
        /// Takes in a module and invokes <see cref="WeaverComponent.VisitModule(ModuleDefinition)"/>
        /// on all components.
        /// </summary>
        public void VisitModule(ModuleDefinition moduleCollection, Log log)
        {
            totalTypesVisited = 0;
            totalMethodsVisited = 0;
            totalFieldsVisited = 0;
            totalPropertiesVisited = 0;

            if (m_ActiveDefinitions != DefinitionType.None)
            {

                // Loop over all sub objects
                for (var componentIndex = m_SubObjects.Length - 1; componentIndex >= 0; componentIndex--)
                {
                    // Assign our type system
                    m_SubObjects[componentIndex].OnBeforeModuleEdited(moduleCollection, log);


                    // Loop over modules if we are editing them
                    if (m_SubObjects[componentIndex].isActive && (m_ActiveDefinitions & DefinitionType.Module) == DefinitionType.Module)
                    {
                        m_SubObjects[componentIndex].VisitModule(moduleCollection);
                    }
                }
                // Visit Types
                VisitTypes(moduleCollection.Types);
                // Loop over all components and invoke our on complete event
                for (var componentIndex = m_SubObjects.Length - 1; componentIndex >= 0; componentIndex--)
                // Invoke that we have complete editing this module
                {
                    m_SubObjects[componentIndex].OnModuleEditComplete(moduleCollection);
                }
            }
        }

        /// <summary>
        /// Takes in a collection of types and invokes <see cref="WeaverComponent.VisitType(TypeDefinition)"/>
        /// on all components.
        /// </summary>
        protected void VisitTypes(Collection<TypeDefinition> typeCollection)
        {
            // We only don't have to visit types if nobody visits properties, methods, or fields.
            if ((m_ActiveDefinitions & ~DefinitionType.Module) != DefinitionType.None)
            {
                for (var typeIndex = typeCollection.Count - 1; typeIndex >= 0; typeIndex--)
                {
                    for (var componentIndex = m_SubObjects.Length - 1; componentIndex >= 0; componentIndex--)
                    {
                        if (m_SubObjects[componentIndex].isActive)
                        {
                            m_SubObjects[componentIndex].VisitType(typeCollection[typeIndex]);
                        }
                    }
                    // visit Methods
                    VisitMethods(typeCollection[typeIndex].Methods);
                    // visit Fields
                    VisitFields(typeCollection[typeIndex].Fields);
                    // visit Properties
                    VisitProperties(typeCollection[typeIndex].Properties);
                    // Increase count
                    totalTypesVisited++;
                }
            }
        }

        /// <summary>
        /// Takes in a collection of types and invokes <see cref="WeaverComponent.VisitMethod(MethodDefinition)"/>
        /// on all components.
        /// </summary>
        protected void VisitMethods(Collection<MethodDefinition> methodCollection)
        {
            // Only visit methods if we have any components that modify them.
            if ((m_ActiveDefinitions & DefinitionType.Method) == DefinitionType.Method)
            {
                for (var methodIndex = methodCollection.Count - 1; methodIndex >= 0; methodIndex--)
                {
                    for (var componentIndex = m_SubObjects.Length - 1; componentIndex >= 0; componentIndex--)
                    {
                        if (m_SubObjects[componentIndex].isActive)
                        {
                            m_SubObjects[componentIndex].VisitMethod(methodCollection[methodIndex]);
                        }
                    }
                    // Increase count
                    totalMethodsVisited++;
                }
            }
        }

        /// <summary>
        /// Takes in a collection of types and invokes <see cref="WeaverComponent.VisitField(FieldDefinition)"/>
        /// on all components.
        /// </summary>
        protected void VisitFields(Collection<FieldDefinition> fieldCollection)
        {
            // Only visit fields if we have any components that modify them.
            if ((m_ActiveDefinitions & DefinitionType.Field) == DefinitionType.Field)
            {
                for (var fieldIndex = fieldCollection.Count - 1; fieldIndex >= 0; fieldIndex--)
                {
                    for (var componentIndex = m_SubObjects.Length - 1; componentIndex >= 0; componentIndex--)
                    {
                        if (m_SubObjects[componentIndex].isActive)
                        {
                            m_SubObjects[componentIndex].VisitField(fieldCollection[fieldIndex]);
                        }
                    }
                    // Increase count
                    totalFieldsVisited++;
                }
            }
        }

        /// <summary>
        /// Takes in a collection of types and invokes <see cref="WeaverComponent.VisitField(FieldDefinition)"/>
        /// on all components.
        /// </summary>
        protected void VisitProperties(Collection<PropertyDefinition> propertyCollection)
        {
            // Only visit properties if we have any components that modify them.
            if ((m_ActiveDefinitions & DefinitionType.Property) == DefinitionType.Property)
            {
                for (var propertyIndex = propertyCollection.Count - 1; propertyIndex >= 0; propertyIndex--)
                {
                    for (var componentIndex = m_SubObjects.Length - 1; componentIndex >= 0; componentIndex--)
                    {
                        if (m_SubObjects[componentIndex].isActive)
                        {
                            m_SubObjects[componentIndex].VisitProperty(propertyCollection[propertyIndex]);
                        }
                    }
                    // Increase count
                    totalPropertiesVisited++;
                }
            }
        }
    }
}
