﻿using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Nest
{
	[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
	[JsonConverter(typeof(ReadAsTypeJsonConverter<IdsQueryDescriptor>))]
	public interface IIdsQuery : IQuery
	{
		[JsonProperty(PropertyName = "types")]
		Types Types { get; set; }

		[JsonProperty(PropertyName = "values")]
		IEnumerable<Id> Values { get; set; }
	}
	
	public class IdsQuery : QueryBase, IIdsQuery
	{
		protected override bool Conditionless => IsConditionless(this);
		public Types Types { get; set; }
		public IEnumerable<Id> Values { get; set; }

		internal override void WrapInContainer(IQueryContainer c) => c.Ids = this;
		internal static bool IsConditionless(IIdsQuery q) => !q.Values.HasAny();
	}

	public class IdsQueryDescriptor 
		: QueryDescriptorBase<IdsQueryDescriptor, IIdsQuery>
		, IIdsQuery
	{
		protected override bool Conditionless => IdsQuery.IsConditionless(this);
		IEnumerable<Id> IIdsQuery.Values { get; set; }
		Types IIdsQuery.Types { get; set; }

		public IdsQueryDescriptor Types(params TypeName[] types) => Assign(a=>a.Types = types);

		public IdsQueryDescriptor Types(IEnumerable<TypeName> values) => Types(values.ToArray());
		
		public IdsQueryDescriptor Types(Types types) => Assign(a=>a.Types = types);

		public IdsQueryDescriptor Values(params Id[] values) => Assign(a => a.Values = values);

		public IdsQueryDescriptor Values(IEnumerable<Id> values) => Values(values.ToArray());
	}
}
