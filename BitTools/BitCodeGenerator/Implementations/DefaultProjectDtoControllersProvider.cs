﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BitTools.Core.Contracts;
using BitTools.Core.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

namespace BitCodeGenerator.Implementations
{
    public class DefaultProjectDtoControllersProvider : IProjectDtoControllersProvider
    {
        public virtual IList<DtoController> GetProjectDtoControllersWithTheirOperations(Project project)
        {
            if (project == null)
                throw new ArgumentNullException(nameof(project));

            IList<DtoController> dtoControllers = new List<DtoController>();

            foreach (Document doc in project.Documents)
            {
                if (!doc.SupportsSemanticModel)
                    continue;

                SemanticModel semanticModel = doc.GetSemanticModelAsync().Result;

                SyntaxNode root = doc.GetSyntaxRootAsync(CancellationToken.None).Result;

                List<ClassDeclarationSyntax> dtoControllersClassDecs = new List<ClassDeclarationSyntax>();

                foreach (ClassDeclarationSyntax classDeclarationSyntax in root.DescendantNodes()
                    .OfType<ClassDeclarationSyntax>())
                {
                    if (classDeclarationSyntax.BaseList == null)
                        continue;

                    INamedTypeSymbol controllerSymbol = (INamedTypeSymbol)semanticModel.GetDeclaredSymbol(classDeclarationSyntax);

                    INamedTypeSymbol type = controllerSymbol;

                    bool isController = false;

                    while (type != null)
                    {
                        isController = type.BaseType?.Name == "DtoController";

                        if (isController == true)
                            break;

                        type = type.BaseType;
                    }

                    if (isController == true)
                        dtoControllersClassDecs.Add(classDeclarationSyntax);
                }

                if (!dtoControllersClassDecs.Any())
                    continue;

                foreach (ClassDeclarationSyntax dtoControllerClassDec in dtoControllersClassDecs)
                {
                    INamedTypeSymbol controllerSymbol = (INamedTypeSymbol)semanticModel.GetDeclaredSymbol(dtoControllerClassDec);

                    DtoController dtoController = new DtoController
                    {
                        ControllerSymbol = controllerSymbol,
                        Name = controllerSymbol.Name.Replace("Controller", string.Empty),
                        Operations = new List<ODataOperation>(),
                        ModelSymbol = controllerSymbol.BaseType.TypeArguments.SingleOrDefault(t => t.IsDto())
                    };

                    if (dtoController.ModelSymbol != null && dtoController.ModelSymbol is ITypeParameterSymbol)
                    {
                        dtoController.ModelSymbol = ((ITypeParameterSymbol)dtoController.ModelSymbol).ConstraintTypes.SingleOrDefault(t => t.IsDto());
                    }

                    if (dtoController.ModelSymbol == null)
                        continue;

                    dtoControllers.Add(dtoController);

                    if (dtoController.ControllerSymbol.IsGenericType)
                        continue;

                    foreach (MethodDeclarationSyntax methodDecSyntax in dtoControllerClassDec.DescendantNodes().OfType<MethodDeclarationSyntax>())
                    {
                        IMethodSymbol methodSymbol = (IMethodSymbol)semanticModel.GetDeclaredSymbol(methodDecSyntax);

                        ImmutableArray<AttributeData> attrs = methodSymbol.GetAttributes();

                        AttributeData actionAttribute = attrs.SingleOrDefault(att => att.AttributeClass.Name == "ActionAttribute");

                        AttributeData functionAttribute = attrs.SingleOrDefault(att => att.AttributeClass.Name == "FunctionAttribute");

                        if (actionAttribute == null && functionAttribute == null)
                            continue;

                        ODataOperation operation = new ODataOperation
                        {
                            Method = methodSymbol,
                            Kind = actionAttribute != null ? ODataOperationKind.Action : ODataOperationKind.Function,
                            ReturnType = methodSymbol.ReturnType
                        };

                        if (operation.Kind == ODataOperationKind.Function)
                        {
                            operation.Parameters = operation.Method.Parameters
                                .Where(p => p.Type.Name != "CancellationToken" && p.Type.Name != "ODataQueryOptions")
                                .Select(parameter => new ODataOperationParameter
                                {
                                    Name = parameter.Name,
                                    Type = parameter.Type
                                }).ToList();
                        }
                        else if (operation.Kind == ODataOperationKind.Action && operation.Method.Parameters.Any())
                        {
                            operation.Parameters = operation.Method.Parameters
                                .Where(p => p.Type.Name != "CancellationToken" && p.Type.Name != "ODataQueryOptions")
                                .Single()
                                .Type
                                .GetMembers()
                                .OfType<IPropertySymbol>()
                                .Select(prop => new ODataOperationParameter
                                {
                                    Name = prop.Name,
                                    Type = prop.Type
                                }).ToList();
                        }

                        dtoController.Operations.Add(operation);
                    }
                }
            }

            return dtoControllers;
        }
    }
}
