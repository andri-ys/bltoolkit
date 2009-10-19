﻿using System;
using System.Data;
using System.Data.Linq.SqlClient;
using System.Linq;
using BLToolkit.DataAccess;
using NUnit.Framework;

using BLToolkit.Data.DataProvider;
using BLToolkit.Data.Linq;

namespace Data.Linq
{
	using Model;

	[TestFixture]
	public class StringFunctions : TestBase
	{
		[Test]
		public void Length()
		{
			ForEachProvider(db => 
			{
				var q = from p in db.Person where p.FirstName.Length == "John".Length && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			});
		}

		[Test]
		public void ContainsConstant()
		{
			ForEachProvider(db => 
			{
				var q = from p in db.Person where p.FirstName.Contains("oh") && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			});
		}

		[Test]
		public void ContainsConstant2()
		{
			ForEachProvider(db => 
			{
				var q = from p in db.Person where !p.FirstName.Contains("o%h") && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			});
		}

		[Test]
		public void ContainsParameter()
		{
			var str = "oh";

			ForEachProvider(db => 
			{
				var q = from p in db.Person where p.FirstName.Contains(str) && p.ID == 1 select new { p, str };
				var r = q.ToList().First();
				Assert.AreEqual(1,   r.p.ID);
				Assert.AreEqual(str, r.str);
			});
		}

		[Test]
		public void ContainsParameter2()
		{
			var str = "o%h";

			ForEachProvider(db => 
			{
				var q = from p in db.Person where !p.FirstName.Contains(str) && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			});
		}

		[Test]
		public void StartsWith()
		{
			ForEachProvider(db => 
			{
				var q = from p in db.Person where p.FirstName.StartsWith("Jo") && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			});
		}

		[Test]
		public void EndsWith()
		{
			ForEachProvider(db => 
			{
				var q = from p in db.Person where p.FirstName.EndsWith("hn") && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			});
		}

		[Test]
		public void Like11()
		{
			ForEachProvider(db => 
			{
				var q = from p in db.Person where SqlMethods.Like(p.FirstName, "%hn%") && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			});
		}

		[Test]
		public void Like12()
		{
			ForEachProvider(db => 
			{
				var q = from p in db.Person where !SqlMethods.Like(p.FirstName, @"%h~%n%", '~') && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			});
		}

		[Test]
		public void Like21()
		{
			ForEachProvider(db => 
			{
				var q = from p in db.Person where Sql.Like(p.FirstName, "%hn%") && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			});
		}

		[Test]
		public void Like22()
		{
			ForEachProvider(db => 
			{
				var q = from p in db.Person where !Sql.Like(p.FirstName, @"%h~%n%", '~') && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			});
		}

		[Test]
		public void IndexOf11()
		{
			ForEachProvider(new[] { ProviderName.Firebird, ProviderName.Informix }, db => 
			{
				var q = from p in db.Person where p.FirstName.IndexOf("oh") == 1 && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			});
		}

		[Test]
		public void IndexOf12()
		{
			ForEachProvider(new[] { ProviderName.Firebird, ProviderName.Informix }, db => 
			{
				var q = from p in db.Person where p.FirstName.IndexOf("") == 0 && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			});
		}

		[Test]
		public void IndexOf2()
		{
			ForEachProvider(new[] { ProviderName.Firebird, ProviderName.Informix }, db => 
			{
				var q = from p in db.Person where p.LastName.IndexOf("e", 2) == 4 && p.ID == 2 select p;
				Assert.AreEqual(2, q.ToList().First().ID);
			});
		}

		[Test]
		public void IndexOf3()
		{
			var s = "e";
			var n1 = 2;
			var n2 = 5;
			ForEachProvider(new[] { ProviderName.DB2, ProviderName.Firebird, ProviderName.Informix, ProviderName.SqlCe, ProviderName.Sybase, ProviderName.Access }, db => 
			{
				var q = from p in db.Person where p.LastName.IndexOf(s, n1, n2) == 1 && p.ID == 2 select p;
				Assert.AreEqual(2, q.ToList().First().ID);
			});
		}

		static readonly string[] _lastIndexExcludeList = new[]
		{
			ProviderName.DB2, ProviderName.Firebird, ProviderName.Informix, ProviderName.SqlCe, ProviderName.Access
		};

		[Test]
		public void LastIndexOf1()
		{
			ForEachProvider(_lastIndexExcludeList, db => 
			{
				var q = from p in db.Person where p.LastName.LastIndexOf("p") == 2 && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			});
		}

		[Test]
		public void LastIndexOf2()
		{
			ForEachProvider(_lastIndexExcludeList, db => 
			{
				var q = from p in db.Person where p.ID == 1 select new { p.ID, FirstName = "123" + p.FirstName + "012345" };
				q = q.Where(p => p.FirstName.LastIndexOf("123", 5) == 8);
				Assert.AreEqual(1, q.ToList().First().ID);
			});
		}

		[Test]
		public void LastIndexOf3()
		{
			ForEachProvider(_lastIndexExcludeList, db => 
			{
				var q = from p in db.Person where p.ID == 1 select new { p.ID, FirstName = "123" + p.FirstName + "0123451234" };
				q = q.Where(p => p.FirstName.LastIndexOf("123", 5, 6) == 8);
				Assert.AreEqual(1, q.ToList().First().ID);
			});
		}

		[Test]
		public void CharIndex1()
		{
			ForEachProvider(new[] { ProviderName.Firebird, ProviderName.Informix }, db => 
			{
				var q = from p in db.Person where Sql.CharIndex("oh", p.FirstName) == 2 && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			});
		}

		[Test]
		public void CharIndex2()
		{
			ForEachProvider(new[] { ProviderName.Firebird, ProviderName.Informix }, db => 
			{
				var q = from p in db.Person where Sql.CharIndex("p", p.LastName, 2) == 3 && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			});
		}

		[Test]
		public void Left()
		{
			ForEachProvider(db => 
			{
				var q = from p in db.Person where Sql.Left(p.FirstName, 2) == "Jo" && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			});
		}

		[Test]
		public void Right()
		{
			ForEachProvider(db => 
			{
				var q = from p in db.Person where Sql.Right(p.FirstName, 3) == "ohn" && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			});
		}

		[Test]
		public void Substring1()
		{
			ForEachProvider(db => 
			{
				var q = from p in db.Person where p.FirstName.Substring(1) == "ohn" && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			});
		}

		[Test]
		public void Substring2()
		{
			ForEachProvider(db => 
			{
				var q = from p in db.Person where p.FirstName.Substring(1, 2) == "oh" && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			});
		}

		[Test]
		public void Reverse()
		{
			ForEachProvider(new[] { ProviderName.DB2, ProviderName.Informix, ProviderName.SqlCe, ProviderName.Access }, db =>
			{
				var q = from p in db.Person where Sql.Reverse(p.FirstName) == "nhoJ" && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			});
		}

		[Test]
		public void Stuff()
		{
			ForEachProvider(db => 
			{
				var q = from p in db.Person where Sql.Stuff(p.FirstName, 3, 1, "123") == "Jo123n" && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			});
		}

		[Test]
		public void Insert()
		{
			ForEachProvider(db => 
			{
				var q = from p in db.Person where p.FirstName.Insert(2, "123") == "Jo123hn" && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			});
		}

		[Test]
		public void Remove1()
		{
			ForEachProvider(db => 
			{
				var q = from p in db.Person where p.FirstName.Remove(2) == "Jo" && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			});
		}

		[Test]
		public void Remove2()
		{
			ForEachProvider(db => 
			{
				var q = from p in db.Person where p.FirstName.Remove(1, 2) == "Jn" && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			});
		}

		[Test]
		public void Space()
		{
			ForEachProvider(db =>
			{
				var q = from p in db.Person where p.FirstName + Sql.Space(p.ID + 1) + "123" == "John  123" && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			});
		}

		[Test]
		public void PadRight()
		{
			ForEachProvider(db =>
			{
				var q = from p in db.Person where Sql.PadRight(p.FirstName, 6, ' ') + "123" == "John  123" && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			});
		}

		[Test]
		public void PadRight1()
		{
			ForEachProvider(db =>
			{
				var q = from p in db.Person where p.FirstName.PadRight(6) + "123" == "John  123" && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			});
		}

		[Test]
		public void PadRight2()
		{
			ForEachProvider(db =>
			{
				var q = from p in db.Person where p.FirstName.PadRight(6, '*') + "123" == "John**123" && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			});
		}

		[Test]
		public void PadLeft()
		{
			ForEachProvider(db =>
			{
				var q = from p in db.Person where "123" + Sql.PadLeft(p.FirstName, 6, ' ') == "123  John" && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			});
		}

		[Test]
		public void PadLeft1()
		{
			ForEachProvider(db =>
			{
				var q = from p in db.Person where "123" + p.FirstName.PadLeft(6) == "123  John" && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			});
		}

		[Test]
		public void PadLeft2()
		{
			ForEachProvider(db =>
			{
				var q = from p in db.Person where "123" + p.FirstName.PadLeft(6, '*') == "123**John" && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			});
		}

		[Test]
		public void Replace()
		{
			ForEachProvider(new[] { ProviderName.Access }, db =>
			{
				var q = from p in db.Person where p.FirstName.Replace("hn", "lie") == "Jolie" && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			});
		}

		[Test]
		public void Trim()
		{
			ForEachProvider(db =>
			{
				var q = 
					from p in db.Person where p.ID == 1 select new { p.ID, Name = "  " + p.FirstName + " " } into pp
					where pp.Name.Trim() == "John" select pp;
				Assert.AreEqual(1, q.ToList().First().ID);
			});
		}

		[Test]
		public void TrimLeft()
		{
			ForEachProvider(db =>
			{
				var q = 
					from p in db.Person where p.ID == 1 select new { p.ID, Name = "  " + p.FirstName + " " } into pp
					where pp.Name.TrimStart() == "John " select pp;
				Assert.AreEqual(1, q.ToList().First().ID);
			});
		}

		[Test]
		public void TrimRight()
		{
			ForEachProvider(db =>
			{
				var q = 
					from p in db.Person where p.ID == 1 select new { p.ID, Name = "  " + p.FirstName + " " } into pp
					where pp.Name.TrimEnd() == "  John" select pp;
				Assert.AreEqual(1, q.ToList().First().ID);
			});
		}

		[Test]
		public void ToLower()
		{
			ForEachProvider(db =>
			{
				var q = from p in db.Person where p.FirstName.ToLower() == "john" && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			});
		}

		[Test]
		public void ToUpper()
		{
			ForEachProvider(db =>
			{
				var q = from p in db.Person where p.FirstName.ToUpper() == "JOHN" && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			});
		}

		[Test]
		public void CompareTo()
		{
			ForEachProvider(db =>
			{
				var q = from p in db.Person where p.FirstName.CompareTo("John") == 0 && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			});
		}

		[Test]
		public void CompareTo1()
		{
			ForEachProvider(db =>
			{
				var q = from p in db.Person where p.FirstName.CompareTo("Joh") > 0 && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			});
		}

		[Test]
		public void CompareTo2()
		{
			ForEachProvider(db =>
			{
				var q = from p in db.Person where p.FirstName.CompareTo("Johnn") < 0 && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			});
		}

		[Test]
		public void CompareTo3()
		{
			ForEachProvider(db =>
			{
				var q = from p in db.Person where p.FirstName.CompareTo(55) > 0 && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			});
		}

		//[Test]
		public void Test()
		{
			using (var db = new TestDbManager(ProviderName.Firebird))
			{
				var p = db
					.SetCommand(@"
						SELECT
							t1.ParentID,
							t1.Value1
						FROM
							Parent t1
								LEFT JOIN (
									SELECT
										t3.ParentID as ParentID1,
										Coalesce(t3.ParentID, 1) as c1
									FROM
										Child t3
								) t2 ON t1.ParentID = t2.ParentID1
						WHERE
							t2.c1 IS NULL")
					.ExecuteList<Parent>();

				var p1 = p.First();
				Assert.AreEqual(1, p1.ParentID);


				var da = new SqlQuery();
				var pr  = (Person)da.SelectByKey(typeof(Person), 1);

				Assert.AreEqual("Pupkin", pr.LastName);


				//Assert.AreEqual(1, p.ID);
			}
		}
	}
}