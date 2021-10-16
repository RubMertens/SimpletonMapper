using NUnit.Framework;
using SimpletonMap.SourceGenerator;

namespace SimpletonMap.Tests.V5
{
     public class Person
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MiddleName { get; set; }
    }
 
     [MappedFrom(typeof(Person))]
    public class PersonViewModel
    {
        public string FirstName { get; set; }
        
        [MapsFrom(nameof(Person.LastName))]
        public string FamilyName { get; set; }

        public string OptionalName { get; set; }
    }

    public class OtherPersonModel
    {
        // [MapsFrom(nameof(Person.LastName))]
        public string FirstName { get; set; }
    }
    
    
    
    public class SimpletonMapperTests
    {
        [Test]
        public void ShouldGenerate_ToPersonViewModel_AndMapCorrectly()
        {
            var fromPerson = new Person()
            {
                FirstName = "John",
                LastName = "Beton",
                MiddleName = "Mixer"
            };
            var toPerson = fromPerson.ToPersonViewModel();

            Assert.AreEqual(fromPerson.FirstName, toPerson.FirstName);
        }
        
        [Test]
        public void ShouldGenerate_From_AndMapCorrectly()
        {
            var fromPerson = new Person()
            {
                FirstName = "John",
                LastName = "Beton",
                MiddleName = "Mixer"
            };
            var toPerson = new PersonViewModel().From(fromPerson);

            Assert.AreEqual(fromPerson.FirstName, toPerson.FirstName);
        }

        [Test]
        public void ShouldGenerate_To_WithAttributeMappingCorrectly()
        {
            var fromPerson = new Person()
            {
                FirstName = "John",
                LastName = "Beton",
                MiddleName = "Mixer"
            };
            var toPerson = fromPerson.ToPersonViewModel();
            
            Assert.AreEqual(fromPerson.LastName, toPerson.FamilyName);
        }
        
        
        [Test]
        public void ShouldGenerate_From_AndMapAttributeCorrectly()
        {
            var fromPerson = new Person()
            {
                FirstName = "John",
                LastName = "Beton",
                MiddleName = "Mixer"
            };
            var toPerson = new PersonViewModel().From(fromPerson);

            Assert.AreEqual(fromPerson.LastName, toPerson.FamilyName);
        }
        
        // [Test]
        // public void ShouldWork_WithExpression()
        // {
        //     var mapper = new SimpletonMapper(MappingStrategy.Roslyn);
        //     mapper
        //         .Register<Person, PersonViewModel>()
        //         .With(p => p.MiddleName, p => p.OptionalName)
        //         ;
        //     
        //     var person = new Person()
        //     {
        //         FirstName = "Jack",
        //         LastName = "Thomsen",
        //         MiddleName = "Markus"
        //     };
        //     mapper.Build();
        //     var viewModel = mapper.Map<PersonViewModel>(person);
        //
        //     Assert.That(viewModel.FirstName, Is.EqualTo(person.FirstName));
        //     Assert.That(viewModel.FamilyName, Is.EqualTo(person.LastName));
        //     Assert.That(viewModel.OptionalName, Is.EqualTo(person.MiddleName));
        // }

        // [Test]
        // public void Should_Respect_Precedence()
        // {
        //     var mapper = new SimpletonMapper(MappingStrategy.Roslyn);
        //
        //     mapper
        //         .Register<Person, OtherPersonModel>()
        //         .With(p => p.MiddleName, p => p.FirstName);
        //     
        //     var person = new Person()
        //     {
        //         FirstName = "Jack",
        //         LastName = "Thomsen",
        //         MiddleName = "Markus"
        //     };
        //
        //     var otherPerson = mapper.Map<OtherPersonModel>(person);
        //     Assert.That(otherPerson.FirstName, Is.EqualTo(person.MiddleName));
        // }

        
    }

    public class Thingy
    {
        public static PersonViewModel Map(Person fromObject)
        {
            return new PersonViewModel()
            {
                FirstName = fromObject.FirstName,
                FamilyName = fromObject.LastName,
                OptionalName = fromObject.MiddleName
            };
        }
    }
}