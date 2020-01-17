using System;
using System.Collections.Generic;

namespace Juniper.Logic
{
    class ItemExpression<ItemT> :
        AbstractUnaryExpression<ItemT, ItemT>
    {
        public ItemExpression(ItemT value)
            : base(value)
        { }

        public static implicit operator ItemExpression<ItemT>(ItemT value)
        {
            return new ItemExpression<ItemT>(value);
        }

        public static implicit operator ItemT(ItemExpression<ItemT> expr)
        {
            if (expr is null)
            {
                return default;
            }

            return expr.Value;
        }

        public override bool Evaluate(ExpressionEvaluator<ItemT> evaluator)
        {
            if (evaluator is null)
            {
                throw new ArgumentNullException(nameof(evaluator));
            }

            return evaluator(Value);
        }

        public override IEnumerable<ItemT> GetItems()
        {
            yield return Value;
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
