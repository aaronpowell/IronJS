﻿using System.Linq.Expressions;

namespace IronJS.Runtime.Js
{
    using Et = System.Linq.Expressions.Expression;

    sealed class Undefined
    {
        static readonly object _sync = new object();
        static Undefined _instance;
        internal static Undefined Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_sync)
                    {
                        if(_instance != null)
                            _instance = new Undefined();
                    }
                }

                return _instance;
            }
        }

        internal static ConstantExpression Expr
        {
            get
            {
                return Et.Constant(Instance);
            }
        }

        private Undefined()
        {

        }

        public override string ToString()
        {
            return "undefined";
        }
    }
}
