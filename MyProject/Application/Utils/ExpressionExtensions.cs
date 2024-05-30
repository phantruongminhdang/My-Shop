using System.Linq.Expressions;

namespace Application.Utils
{
    public static class ExpressionExtensions
    {
        public static Expression<Func<T, bool>> AndAlso<T>(
            this Expression<Func<T, bool>> left,
            Expression<Func<T, bool>> right
            )
        {
            var andAlso = Expression.AndAlso(
                left.Body,
                new ReplaceParameterVisitor(right.Parameters[0], left.Parameters[0]).Visit(right.Body)
            );

            return Expression.Lambda<Func<T, bool>>(andAlso, left.Parameters);
        }

        private class ReplaceParameterVisitor : ExpressionVisitor
        {
            private readonly ParameterExpression _oldParameter;
            private readonly ParameterExpression _newParameter;

            public ReplaceParameterVisitor(ParameterExpression oldParameter, ParameterExpression newParameter)
            {
                _oldParameter = oldParameter;
                _newParameter = newParameter;
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                return node == _oldParameter ? _newParameter : base.VisitParameter(node);
            }
        }
    }
}
