﻿using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Bit.SourceGenerators;

public class AutoInjectSyntaxReceiver : ISyntaxContextReceiver
{
    public Collection<ISymbol> EligibleMembers { get; } = new();
    public Collection<INamedTypeSymbol> EligibleClassesWithBaseClassUsedAutoInject { get; } = new();

    public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
    {
        MarkEligibleFields(context);
        MarkEligibleProperties(context);
        MarkEligibleClasses(context);
    }

    private void MarkEligibleClasses(GeneratorSyntaxContext context)
    {
        if ((context.Node is ClassDeclarationSyntax classDeclarationSyntax) is false)
            return;

        var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax);

        if (classSymbol == null)
            return;

        if (classSymbol.BaseType == null)
            return;

        if (classSymbol.BaseType.ToDisplayString() == "System.Object")
            return;

        var isBaseTypeUseAutoInject = classSymbol.BaseType
                        .GetMembers()
                        .Any(m =>
                            (m.Kind == SymbolKind.Field || m.Kind == SymbolKind.Property) &&
                             m.GetAttributes()
                                .Any(a => a.AttributeClass != null &&
                                          a.AttributeClass.ToDisplayString() == AutoInjectHelper.AutoInjectAttributeFullName));

        var isCurrentClassUseAutoInject = classSymbol
                        .GetMembers()
                        .Any(m =>
                            (m.Kind == SymbolKind.Field || m.Kind == SymbolKind.Property) &&
                             m.GetAttributes()
                                .Any(a => a.AttributeClass != null &&
                                          a.AttributeClass.ToDisplayString() == AutoInjectHelper.AutoInjectAttributeFullName));

        if (isBaseTypeUseAutoInject && (isCurrentClassUseAutoInject is false))
            EligibleClassesWithBaseClassUsedAutoInject.Add(classSymbol);
    }

    private void MarkEligibleFields(GeneratorSyntaxContext context)
    {
        if ((context.Node is FieldDeclarationSyntax fieldDeclarationSyntax) is false ||
            fieldDeclarationSyntax.AttributeLists.Any() is false) return;

        var classDeclarationSyntax = (ClassDeclarationSyntax?)fieldDeclarationSyntax.Parent;

        if (classDeclarationSyntax is null)
            return;

        foreach (VariableDeclaratorSyntax variable in fieldDeclarationSyntax.Declaration.Variables)
        {
            var fieldSymbol = ModelExtensions.GetDeclaredSymbol(context.SemanticModel, variable) as IFieldSymbol;
            if (fieldSymbol != null &&
                fieldSymbol.GetAttributes()
                    .Any(ad => ad.AttributeClass != null &&
                               ad.AttributeClass.ToDisplayString() == AutoInjectHelper.AutoInjectAttributeFullName))
            {
                EligibleMembers.Add(fieldSymbol);
            }
        }
    }

    private void MarkEligibleProperties(GeneratorSyntaxContext context)
    {
        if (!(context.Node is PropertyDeclarationSyntax propertyDeclarationSyntax) ||
            propertyDeclarationSyntax.AttributeLists.Count <= 0) return;

        var classDeclarationSyntax = (ClassDeclarationSyntax?)propertyDeclarationSyntax.Parent;

        if (classDeclarationSyntax is null) return;

        var propertySymbol = context.SemanticModel.GetDeclaredSymbol(propertyDeclarationSyntax);

        if (propertySymbol is null) return;

        if (propertySymbol.GetAttributes().Any(ad => ad.AttributeClass != null && ad.AttributeClass.ToDisplayString() == AutoInjectHelper.AutoInjectAttributeFullName))
        {
            EligibleMembers.Add(propertySymbol);
        }
    }
}
