using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Zenject.Analyzers;

internal static class RoslynExtensions
{
    /// <summary>
    /// A helper to replace an existing node with a new node in a <see cref="Document"/>'s syntax tree.
    /// </summary>
    public static async Task<Document> ReplaceNodeAsync<TNode>(
        this Document document,
        TNode oldNode,
        TNode newNode,
        CancellationToken cancellationToken)
        where TNode : SyntaxNode
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null) return document;
        var newRoot = root.ReplaceNode(oldNode, newNode);
        return document.WithSyntaxRoot(newRoot);
    }
}