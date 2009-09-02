﻿using System;
using System.Collections.Generic;
using System.Reflection;

namespace BLToolkit.Data.Sql.SqlProvider
{
	using DataProvider;

#if FW3
	using Linq;

	using C = Char;
	using S = String;
	using I = Int32;
#endif

	public class MSSqlSqlProvider : BasicSqlProvider
	{
		public MSSqlSqlProvider(DataProviderBase dataProvider) : base(dataProvider)
		{
		}

		protected override string TopFormat { get { return " TOP {0} "; } }

		public override ISqlExpression ConvertExpression(ISqlExpression expr)
		{
			expr = base.ConvertExpression(expr);

			if (expr is SqlFunction)
			{
				SqlFunction func = (SqlFunction) expr;

				switch (func.Name)
				{
					case "Length" : return new SqlFunction("Len", func.Parameters);
				}
			}

			return expr;
		}

#if FW3
		protected override Dictionary<MemberInfo,BaseExpressor> GetExpressors() { return _members; }
		static    readonly Dictionary<MemberInfo,BaseExpressor> _members = new Dictionary<MemberInfo,BaseExpressor>
		{
			{ MI(() => Sql.PadRight("",0,' ')), new F<S,I,C,S>((p0,p1,p2) => p0.Length > p1 ? p0 : p0 + Replicate(p2, p1 - p0.Length)) },
			{ MI(() => Sql.PadLeft ("",0,' ')), new F<S,I,C,S>((p0,p1,p2) => p0.Length > p1 ? p0 : Replicate(p2, p1 - p0.Length) + p0) },
			{ MI(() => Sql.Trim    ("")      ), new F<S,S>    ( p0        => Sql.TrimLeft(Sql.TrimRight(p0))) },
		};
#endif
	}
}
