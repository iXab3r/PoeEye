using System.Collections.ObjectModel;

namespace PoeShared.Expressions;

public class ExpressionEqualityComparer : IEqualityComparer<Expression>
{
    public static ExpressionEqualityComparer Instance = new ExpressionEqualityComparer();

    public bool Equals(Expression a, Expression b)
    {
        return new ExpressionComparison(a, b).AreEqual;
    }

    public int GetHashCode(Expression expression)
    {
        return new HashCodeCalculation(expression).HashCode;
    }
}

internal class HashCodeCalculation : ExpressionVisitor
{
    private int hashCode;

    public int HashCode
    {
        get { return hashCode; }
    }

    public HashCodeCalculation(Expression expression)
    {
        Visit(expression);
    }

    private void Add(int i)
    {
        hashCode *= 37;
        hashCode ^= i;
    }

    public override Expression Visit(Expression node)
    {
        if (node == null) return null;

        Add((int) node.NodeType);
        Add(node.Type.GetHashCode());

        return base.Visit(node);
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        if (node != null && node.Value != null) Add(node.Value.GetHashCode());
        return node;
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        Add(node.Member.GetHashCode());
        return base.VisitMember(node);
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        Add(node.Method.GetHashCode());
        return base.VisitMethodCall(node);
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        Add(node.Name.GetHashCode());
        return node;
    }

    protected override Expression VisitTypeBinary(TypeBinaryExpression node)
    {
        Add(node.TypeOperand.GetHashCode());
        return base.VisitTypeBinary(node);
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        if (node.Method != null) Add(node.Method.GetHashCode());
        if (node.IsLifted) Add(1);
        if (node.IsLiftedToNull) Add(1);

        return base.VisitBinary(node);
    }

    protected override Expression VisitUnary(UnaryExpression node)
    {
        if (node.Method != null) Add(node.Method.GetHashCode());
        if (node.IsLifted) Add(1);
        if (node.IsLiftedToNull) Add(1);

        return base.VisitUnary(node);
    }

    protected override Expression VisitNew(NewExpression node)
    {
        Add(node.Constructor.GetHashCode());
        foreach (var member in node.Members)
        {
            if (member != null) Add(member.GetHashCode());
        }

        return base.VisitNew(node);
    }
}

internal class ExpressionComparison : ExpressionVisitor
{
    private bool _areEqual = true;

    private Queue<Expression> _candidates;
    private Expression _candidate;

    public bool AreEqual
    {
        get { return _areEqual; }
    }

    public ExpressionComparison(Expression a, Expression b)
    {
        _candidates = new Queue<Expression>(new ExpressionEnumeration(b));

        Visit(a);

        if (_candidates.Count > 0) Stop();
    }

    private Expression PeekCandidate()
    {
        if (_candidates.Count == 0) return null;
        return _candidates.Peek();
    }

    private Expression PopCandidate()
    {
        return _candidates.Dequeue();
    }

    private bool CheckAreOfSameType(Expression candidate, Expression expression)
    {
        if (!CheckEqual(expression.NodeType, candidate.NodeType)) return false;
        if (!CheckEqual(expression.Type, candidate.Type)) return false;

        return true;
    }

    private void Stop()
    {
        _areEqual = false;
    }

    private T CandidateFor<T>(T original) where T : Expression
    {
        return (T) _candidate;
    }

    public override Expression Visit(Expression node)
    {
        if (node == null) return null;
        if (!_areEqual) return node;

        _candidate = PeekCandidate();
        if (!CheckNotNull(_candidate)) return node;
        if (!CheckAreOfSameType(_candidate, node)) return node;

        PopCandidate();

        return base.Visit(node);
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        var candidate = CandidateFor(node);
        if (!CheckEqual(node.Value, candidate.Value)) return node;
        return node;
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        var candidate = CandidateFor(node);
        if (!CheckEqual(node.Member, candidate.Member)) return node;
        return base.VisitMember(node);
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        var candidate = CandidateFor(node);
        if (!CheckEqual(node.Method, candidate.Method)) return node;
        return base.VisitMethodCall(node);
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        var candidate = CandidateFor(node);
        if (!CheckEqual(node.Name, candidate.Name)) return node;
        return node;
    }

    protected override Expression VisitTypeBinary(TypeBinaryExpression node)
    {
        var candidate = CandidateFor(node);
        if (!CheckEqual(node.TypeOperand, candidate.TypeOperand)) return node;
        return base.VisitTypeBinary(node);
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        var candidate = CandidateFor(node);
        if (!CheckEqual(node.Method, candidate.Method)) return node;
        if (!CheckEqual(node.IsLifted, candidate.IsLifted)) return node;
        if (!CheckEqual(node.IsLiftedToNull, candidate.IsLiftedToNull)) return node;

        return base.VisitBinary(node);
    }

    protected override Expression VisitUnary(UnaryExpression node)
    {
        var candidate = CandidateFor(node);
        if (!CheckEqual(node.Method, candidate.Method)) return node;
        if (!CheckEqual(node.IsLifted, candidate.IsLifted)) return node;
        if (!CheckEqual(node.IsLiftedToNull, candidate.IsLiftedToNull)) return node;

        return base.VisitUnary(node);
    }

    protected override Expression VisitNew(NewExpression node)
    {
        var candidate = CandidateFor(node);
        if (!CheckEqual(node.Constructor, candidate.Constructor)) return node;
        CompareList(node.Members, candidate.Members);

        return base.VisitNew(node);
    }


    private void CompareList<T>(ReadOnlyCollection<T> collection, ReadOnlyCollection<T> candidates)
    {
        CompareList(collection, candidates, (item, candidate) => EqualityComparer<T>.Default.Equals(item, candidate));
    }

    private void CompareList<T>(ReadOnlyCollection<T> collection, ReadOnlyCollection<T> candidates, Func<T, T, bool> comparer)
    {
        if (!CheckAreOfSameSize(collection, candidates)) return;

        for (int i = 0; i < collection.Count; i++)
        {
            if (!comparer(collection[i], candidates[i]))
            {
                Stop();
                return;
            }
        }
    }

    private bool CheckAreOfSameSize<T>(ReadOnlyCollection<T> collection, ReadOnlyCollection<T> candidate)
    {
        return CheckEqual(collection.Count, candidate.Count);
    }

    private bool CheckNotNull<T>(T t) where T : class
    {
        if (t == null)
        {
            Stop();
            return false;
        }

        return true;
    }

    private bool CheckEqual<T>(T t, T candidate)
    {
        if (!EqualityComparer<T>.Default.Equals(t, candidate))
        {
            Stop();
            return false;
        }

        return true;
    }
}

internal class ExpressionEnumeration : ExpressionVisitor, IEnumerable<Expression>
{
    private List<Expression> _expressions = new List<Expression>();

    public ExpressionEnumeration(Expression expression)
    {
        Visit(expression);
    }

    public override Expression Visit(Expression node)
    {
        if (node == null) return null;

        _expressions.Add(node);
        return base.Visit(node);
    }

    public IEnumerator<Expression> GetEnumerator()
    {
        return _expressions.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
