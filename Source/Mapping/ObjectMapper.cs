using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

using BLToolkit.Reflection;

namespace BLToolkit.Mapping
{
	public class ObjectMapper : IMapDataSource, IMapDataDestination
	{
		#region Constructor

		public ObjectMapper()
		{
			_members      = new ArrayList();
			_nameToMember = new Hashtable();
		}

		#endregion

		#region Protected Members

		protected virtual MemberMapper CreateMemberMapper(MapMemberInfo mapMemberInfo)
		{
			if (mapMemberInfo == null) throw new ArgumentNullException("mapMemberInfo");

			MemberMapper mm = null;

			Attribute attr = mapMemberInfo.MemberAccessor.GetAttribute(typeof(MemberMapperAttribute));

			if (attr != null)
				mm = ((MemberMapperAttribute)attr).MemberMapper;

			if (mm == null)
			{
				object[] attrs = TypeHelper.GetAttributes(
					mapMemberInfo.MemberAccessor.MemberInfo.DeclaringType, typeof(MemberMapperAttribute));

				foreach (MemberMapperAttribute a in attrs)
				{
					if (a.MemberType == mapMemberInfo.MemberAccessor.Type)
					{
						mm = a.MemberMapper;
						break;
					}
				}
			}

			if (mm == null)
				mm = MemberMapper.CreateMemberMapper(mapMemberInfo);

			mm.Init(mapMemberInfo);

			return mm;
		}

		[SuppressMessage("Microsoft.Performance", "CA1807:AvoidUnnecessaryStringCreation", MessageId = "stack1")]
		protected void Add(MemberMapper memberMapper)
		{
			if (memberMapper == null) throw new ArgumentNullException("memberMapper");

			memberMapper.SetOrdinal(_members.Count);

			_members.     Add(memberMapper);
			_nameToMember.Add(memberMapper.Name.ToLower(),  memberMapper);
		}

		private   ArrayList _members;
		protected MemberMapper this[int index]
		{
			get { return (MemberMapper)_members[index]; }
		}

		private   Hashtable _nameToMember;
		protected MemberMapper this[string name]
		{
			get
			{
				if (name == null) throw new ArgumentNullException("name");

				MemberMapper mm = (MemberMapper)_nameToMember[name];

				if (mm == null)
				{
					mm = (MemberMapper)_nameToMember[name.ToLower(CultureInfo.CurrentCulture)];

					if (mm == null)
					{
						mm = GetComplexMapper(name, name);

						if (mm != null)
							Add(mm);
					}
					else
					{
						_nameToMember[name] = mm;
					}
				}

				return mm;
			}
		}

		private   TypeAccessor _typeAccessor;
		protected TypeAccessor  TypeAccessor
		{
			get { return _typeAccessor; }
		}

		private   MappingSchema _mappingSchema;
		protected MappingSchema  MappingSchema
		{
			get { return _mappingSchema; }
		}

		private   object[] _mapFieldAttributes;
		protected object[]  MapFieldAttributes
		{
			get
			{
				if (_mapFieldAttributes == null)
					_mapFieldAttributes = TypeHelper.GetAttributes(_typeAccessor.Type, typeof(MapFieldAttribute));

				return _mapFieldAttributes;
			}
		}

		#endregion

		#region Init Mapper

		public virtual void Init(MappingSchema mappingSchema, Type type)
		{
			if (type == null) throw new ArgumentNullException("type");

			_typeAccessor  = TypeAccessor.GetAccessor(type);
			_mappingSchema = mappingSchema;

			foreach (MemberAccessor ma in _typeAccessor)
			{
				if (GetIgnore(ma))
					continue;

				MapMemberInfo mi = new MapMemberInfo();

				mi.MemberAccessor = ma;
				mi.MappingSchema  = mappingSchema;
				mi.Name           = GetFieldName(ma);
				mi.IsTrimmable    = GetIsTrimmable(ma);
				mi.MapValues      = GetMapValues(ma);
				mi.DefaultValue   = GetDefaultValue(ma);
				mi.IsNullable     = GetIsNullable(ma);
				mi.NullValue      = GetNullValue(ma, mi.IsNullable);

				Add(CreateMemberMapper(mi));
			}

			foreach (MapFieldAttribute attr in MapFieldAttributes)
				EnsureMapper(attr.MapName, attr.OrigName);
		}

		private MemberMapper EnsureMapper(string mapName, string origName)
		{
			MemberMapper mm = this[mapName];

			if (mm == null)
			{
				string name = mapName.ToLower();

				foreach (MemberMapper m in _members)
				{
					if (m.MemberAccessor.Name.ToLower() == name)
					{
						_nameToMember.Add(name,  m);
						return m;
					}
				}

				mm = GetComplexMapper(mapName, origName);

				if (mm != null)
					Add(mm);
			}

			return mm;
		}

		[SuppressMessage("Microsoft.Performance", "CA1807:AvoidUnnecessaryStringCreation", MessageId = "stack0")]
		[SuppressMessage("Microsoft.Performance", "CA1807:AvoidUnnecessaryStringCreation", MessageId = "origName")]
		protected MemberMapper GetComplexMapper(string mapName, string origName)
		{
			if (origName == null) throw new ArgumentNullException("origName");

			string name = origName.ToLower();
			int    idx  = origName.IndexOf('.');

			if (idx > 0 /*&& idx < origName.Length*/)
			{
				name = name.Substring(0, idx);

				foreach (MemberAccessor ma in TypeAccessor)
				{
					if (ma.Name.ToLower() == name)
					{
						ObjectMapper om = MappingSchema.GetObjectMapper(ma.Type);

						if (om != null)
						{
							MemberMapper mm = om.GetComplexMapper(mapName, origName.Substring(idx + 1));

							if (mm != null)
							{
								MapMemberInfo mi = new MapMemberInfo();

								mi.MemberAccessor = ma;
								mi.MappingSchema  = MappingSchema;
								mi.Name           = mapName;

								MemberMapper mapper = new MemberMapper.ComplexMapper(mm);

								mapper.Init(mi);

								return mapper;
							}
						}

						break;
					}
				}
			}
			else
				foreach (MemberMapper m in _members)
					if (m.MemberAccessor.Name.ToLower() == name)
						return m;

			return null;
		}

		protected virtual string GetFieldName(MemberAccessor memberAccessor)
		{
			MapFieldAttribute a = (MapFieldAttribute)memberAccessor.GetAttribute(typeof(MapFieldAttribute));

			if (a != null)
				return a.MapName;

			string name = memberAccessor.Name.ToLower();

			foreach (MapFieldAttribute attr in MapFieldAttributes)
				if (attr.OrigName.ToLower() == name)
					return attr.MapName;

			return memberAccessor.Name;
		}

		protected virtual bool GetIgnore(MemberAccessor memberAccessor)
		{
			MapIgnoreAttribute attr = 
				(MapIgnoreAttribute)memberAccessor.GetAttribute(typeof(MapIgnoreAttribute));

			if (attr != null)
				return attr.Ignore;

			Type type = memberAccessor.Type;

			return type.IsClass && type != typeof(string);
		}

		protected virtual bool GetIsTrimmable(MemberAccessor memberAccessor)
		{
			if (memberAccessor.Type != typeof(string))
				return false;

			TrimmableAttribute attr = 
				(TrimmableAttribute)memberAccessor.GetAttribute(typeof(TrimmableAttribute));

			if (attr != null)
				return attr.IsTrimmable;

			attr = (TrimmableAttribute)TypeHelper.GetFirstAttribute(
				memberAccessor.MemberInfo.DeclaringType, typeof(TrimmableAttribute));

			if (attr != null)
				return attr.IsTrimmable;

			return TrimmableAttribute.Default.IsTrimmable;
		}

		protected virtual MapValue[] GetMapValues(MemberAccessor memberAccessor)
		{
			ArrayList list  = null;
			object[]  attrs = memberAccessor.GetAttributes(typeof(MapValueAttribute));

			if (attrs != null)
			{
				list = new ArrayList(attrs.Length);

				foreach (MapValueAttribute a in attrs)
					list.Add(new MapValue(a.OrigValue, a.Values));
			}

			attrs = memberAccessor.GetTypeAttributes(typeof(MapValueAttribute));

			if (attrs != null && attrs.Length > 0)
			{
				if (list == null)
					list = new ArrayList(attrs.Length);

				foreach (MapValueAttribute a in attrs)
					if (a.Type == null && a.OrigValue != null && a.OrigValue.GetType() == memberAccessor.Type ||
						a.Type != null && a.Type == memberAccessor.Type)
						list.Add(new MapValue(a.OrigValue, a.Values));
			}

			MapValue[] typeMapValues = _mappingSchema.GetMapValues(memberAccessor.Type);

			if (list == null) return typeMapValues;

			if (typeMapValues != null)
				list.AddRange(typeMapValues);

			return (MapValue[])list.ToArray(typeof(MapValue));
		}

		protected virtual object GetDefaultValue(MemberAccessor memberAccessor)
		{
			// Check member [DefaultValue(0)]
			//
			DefaultValueAttribute attr =
				(DefaultValueAttribute)memberAccessor.GetAttribute(typeof(DefaultValueAttribute));

			if (attr != null)
				return attr.Value;

			// Check type [DefaultValues(typeof(int), 0)]
			//
			object[] attrs = memberAccessor.GetTypeAttributes(typeof(DefaultValueAttribute));

			foreach (DefaultValueAttribute a in attrs)
				if (a.Type == null && a.Value != null && a.Value.GetType() == memberAccessor.Type ||
					a.Type != null && a.Type == memberAccessor.Type)
					return a.Value;

			return _mappingSchema.GetDefaultValue(memberAccessor.Type);
		}

		protected virtual bool GetIsNullable(MemberAccessor memberAccessor)
		{
			// Check member [Nullable(true | false)]
			//
			NullableAttribute attr1 =
				(NullableAttribute)memberAccessor.GetAttribute(typeof(NullableAttribute));

			if (attr1 != null)
				return attr1.IsNullable;

			// Check member [NullValue(0)]
			//
			NullValueAttribute attr2 =
				(NullValueAttribute)memberAccessor.GetAttribute(typeof(NullValueAttribute));

			if (attr2 != null)
				return true;

			// Check type [Nullable(true || false)]
			//
			attr1 = (NullableAttribute)TypeHelper.GetFirstAttribute(
				memberAccessor.MemberInfo.DeclaringType, typeof(NullableAttribute));

			if (attr1 != null)
				return attr1.IsNullable;

			// Check type [NullValues(typeof(int), 0)]
			//
			object[] attrs = memberAccessor.GetTypeAttributes(typeof(NullValueAttribute));

			foreach (NullValueAttribute a in attrs)
				if (a.Type == null && a.Value != null && a.Value.GetType() == memberAccessor.Type ||
					a.Type != null && a.Type == memberAccessor.Type)
					return true;

			if (memberAccessor.Type.IsEnum)
				return MappingSchema.GetNullValue(memberAccessor.Type) != null;

			return false;
		}

		[SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
		private static object CheckNullValue(object value, MemberAccessor memberAccessor)
		{
			if (value is Type && (Type)value == typeof(DBNull))
			{
				value = DBNull.Value;

				if (memberAccessor.Type == typeof(string))
					value = null;
			}

			return value;
		}

		protected virtual object GetNullValue(MemberAccessor memberAccessor, bool isNullable)
		{
			if (isNullable)
			{
				// Check member [NullValue(0)]
				//
				NullValueAttribute attr =
					(NullValueAttribute)memberAccessor.GetAttribute(typeof(NullValueAttribute));

				if (attr != null)
					return CheckNullValue(attr.Value, memberAccessor);

				// Check type [NullValues(typeof(int), 0)]
				//
				object[] attrs = memberAccessor.GetTypeAttributes(typeof(NullValueAttribute));

				foreach (NullValueAttribute a in attrs)
					if (a.Type == null && a.Value != null && a.Value.GetType() == memberAccessor.Type ||
						a.Type != null && a.Type == memberAccessor.Type)
						return CheckNullValue(a.Value, memberAccessor);
			}

			return CheckNullValue(MappingSchema.GetNullValue(memberAccessor.Type), memberAccessor);
		}

		#endregion

		#region IObjectMappper Members

		public virtual object CreateInstance()
		{
			return _typeAccessor.CreateInstanceEx();
		}

		public virtual object CreateInstance(InitContext context)
		{
			return _typeAccessor.CreateInstanceEx(context);
		}

		#endregion

		#region IMapDataSource Members

		public virtual int Count
		{
			get { return _members.Count; }
		}

		public virtual string GetName(int index)
		{
			return ((MemberMapper)_members[index]).Name;
		}

		public virtual object GetValue(object o, int index)
		{
			return ((MemberMapper)_members[index]).GetValue(o);
		}

		public virtual object GetValue(object o, string name)
		{
			MemberMapper mm = (MemberMapper)_nameToMember[name];

			if (mm == null)
				mm = this[name];

			return mm == null? null: mm.GetValue(o);
		}

		#endregion

		#region IMapDataDestination Members

		public virtual int GetOrdinal(string name)
		{
			MemberMapper mm = (MemberMapper)_nameToMember[name];

			if (mm == null)
				mm = this[name];

			return mm == null? -1: mm.Ordinal;
		}

		public virtual void SetValue(object o, int index, object value)
		{
			((MemberMapper)_members[index]).SetValue(o, value);
		}

		public virtual void SetValue(object o, string name, object value)
		{
			SetValue(o, GetOrdinal(name), value);
		}

		#endregion
	}
}