﻿using System;
using System.Linq;
using System.Linq.Expressions;
using Nest;
using Newtonsoft.Json;
using Tests.Framework;
using Tests.Framework.MockData;
using static Tests.Framework.RoundTripper;
using static Nest.Infer;
using Field = Nest.Field;

namespace Tests.ClientConcepts.HighLevel.Inferrence.FieldNames
{
	public class FieldInferrence
	{
		/** # Strongly typed field access 
		 * 
		 * Several places in the elasticsearch API expect the path to a field from your original source document as a string.
		 * NEST allows you to use C# expressions to strongly type these field path strings. 
		 *
		 * These expressions are assigned to a type called `Field` and there are several ways to create a instance of that type
		 */

		/** Using the constructor directly is possible but rather involved */
		[U] public void UsingConstructors()
		{
			var fieldString = new Field {Name = "name"};

			/** especially when using C# expressions since these can not be simply new'ed*/
			Expression<Func<Project, object>> expression = p => p.Name;
			var fieldExpression = Field.Create(expression);

			Expect("name")
				.WhenSerializing(fieldExpression)
				.WhenSerializing(fieldString);
		}
		
		/** Therefor you can also implicitly convert strings and expressions to Field's */
		[U] public void ImplicitConversion()
		{
			Field fieldString = "name";

			/** but for expressions this is still rather involved */
			Expression<Func<Project, object>> expression = p => p.Name;
			Field fieldExpression = expression;

			Expect("name")
				.WhenSerializing(fieldExpression)
				.WhenSerializing(fieldString);
		}

		/** to ease creating Field's from expressions there is a static Property class you can use */
		[U] public void UsingStaticPropertyField()
		{
			Field fieldString = "name";

			/** but for expressions this is still rather involved */
			var fieldExpression = Field<Project>(p=>p.Name);

			/** Using static imports in c# 6 this can be even shortened:
				using static Nest.Static; 
			*/
			fieldExpression = Field<Project>(p=>p.Name);
			/** Now this is much much terser then our first example using the constructor! */

			Expect("name")
				.WhenSerializing(fieldString)
				.WhenSerializing(fieldExpression);
		}
		
		/** By default NEST will camelCase all the field names to be more javascripty */
		[U] public void DefaultFieldNameInferrer()
		{
			/** using DefaultFieldNameInferrer() on ConnectionSettings you can change this behavior */
			var setup = WithConnectionSettings(s => s.DefaultFieldNameInferrer(p => p.ToUpper()));

			setup.Expect("NAME").WhenSerializing(Field<Project>(p => p.Name));

			/** However string are *always* passed along verbatim */
			setup.Expect("NaMe").WhenSerializing<Field>("NaMe");

			/** if you want the same behavior for expressions simply do nothing in the default inferrer */
			setup = WithConnectionSettings(s => s.DefaultFieldNameInferrer(p => p));
			setup.Expect("Name").WhenSerializing(Field<Project>(p => p.Name));
		}

		/** Complex field name expressions */

		[U] public void ComplexFieldNameExpressions()
		{
			/** You can follow your property expression to any depth, here we are traversing to the LeadDeveloper's (Person) FirstName */
			Expect("leadDeveloper.firstName").WhenSerializing(Field<Project>(p => p.LeadDeveloper.FirstName));
			/** When dealing with collection index access is ingnored allowing you to traverse into properties of collections */
			Expect("curatedTags").WhenSerializing(Field<Project>(p => p.CuratedTags[0]));
			/** Similarly .First() also works, remember these are expressions and not actual code that will be executed */
			Expect("curatedTags").WhenSerializing(Field<Project>(p => p.CuratedTags.First()));
			Expect("curatedTags.added").WhenSerializing(Field<Project>(p => p.CuratedTags[0].Added));
			Expect("curatedTags.name").WhenSerializing(Field<Project>(p => p.CuratedTags.First().Name));
			
			/** When we see an indexer on a dictionary we assume they describe property names */
			Expect("metadata.hardcoded").WhenSerializing(Field<Project>(p => p.Metadata["hardcoded"]));
			Expect("metadata.hardcoded.created").WhenSerializing(Field<Project>(p => p.Metadata["hardcoded"].Created));

			/** A cool feature here is that we'll evaluate variables passed to these indexers */
			var variable = "var";
			Expect("metadata.var").WhenSerializing(Field<Project>(p => p.Metadata[variable]));
			Expect("metadata.var.created").WhenSerializing(Field<Project>(p => p.Metadata[variable].Created));


			/** If you are using elasticearch's multifield mapping (you really should!) these "virtual" sub fields 
			* do not always map back on to your POCO, by calling .Suffix() you describe the sub fields that do not live in your c# objects
			*/
			Expect("leadDeveloper.firstName.raw").WhenSerializing(Field<Project>(p => p.LeadDeveloper.FirstName.Suffix("raw")));
			Expect("curatedTags.raw").WhenSerializing(Field<Project>(p => p.CuratedTags[0].Suffix("raw")));
			Expect("curatedTags.raw").WhenSerializing(Field<Project>(p => p.CuratedTags.First().Suffix("raw")));
			Expect("curatedTags.added.raw").WhenSerializing(Field<Project>(p => p.CuratedTags[0].Added.Suffix("raw")));
			Expect("metadata.hardcoded.raw").WhenSerializing(Field<Project>(p => p.Metadata["hardcoded"].Suffix("raw")));
			Expect("metadata.hardcoded.created.raw").WhenSerializing(Field<Project>(p => p.Metadata["hardcoded"].Created.Suffix("raw")));

			/**
			* You can even chain them to any depth!
			*/
			Expect("curatedTags.name.raw.evendeeper").WhenSerializing(Field<Project>(p => p.CuratedTags.First().Name.Suffix("raw").Suffix("evendeeper")));


			/** Variables passed to suffix will be evaluated as well */
			var suffix = "unanalyzed";
			Expect("metadata.var.unanalyzed").WhenSerializing(Field<Project>(p => p.Metadata[variable].Suffix(suffix)));
			Expect("metadata.var.created.unanalyzed").WhenSerializing(Field<Project>(p => p.Metadata[variable].Created.Suffix(suffix)));
		}

		/** Annotations 
		* 
		* When using NEST's property attributes you can specify a new name for the properties
		*/
		public class BuiltIn
		{
			[String(Name="naam")]
			public string Name { get; set; }
		}
		[U] public void BuiltInAnnotiatons()
		{
			Expect("naam").WhenSerializing(Field<BuiltIn>(p=>p.Name));
		}
		
		/** 
		* Starting with NEST 2.x we also ask the serializer if it can resolve the property to a name.
		* Here we ask the default JsonNetSerializer and it takes JsonProperty into account
		*/
		public class SerializerSpecific
		{
			[JsonProperty("nameInJson")]
			public string Name { get; set; }
		}
		[U] public void SerializerSpecificAnnotations()
		{
			Expect("nameInJson").WhenSerializing(Field<SerializerSpecific>(p=>p.Name));
		}

		/** 
		* If both are specified NEST takes precedence though 
		*/
		public class Both
		{
			[String(Name="naam")]
			[JsonProperty("nameInJson")]
			public string Name { get; set; }
		}
		[U] public void NestAttributeTakesPrecedence()
		{
			Expect("naam").WhenSerializing(Field<Both>(p=>p.Name));
			Expect(new
			{
				naam = "Martijn Laarman"
			}).WhenSerializing(new Both { Name = "Martijn Laarman" });
		}
	}
}
