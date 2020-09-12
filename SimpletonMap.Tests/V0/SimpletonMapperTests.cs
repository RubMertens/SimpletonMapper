using NUnit.Framework;
using SimpletonMap.V0;

namespace SimpletonMap.Tests.V0
{
    public class Person
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
    
    public class PersonViewModel
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
    
    public class SimpletonMapperTests
    {
        [Test]
        public void ShouldWork()
        {
            var mapper = new SimpletonMapper();
            mapper.Register<Person, PersonViewModel>();
            var person = new Person()
            {
                FirstName = "Jack",
                LastName = "Thomsen"
            };
            var viewModel = mapper.Map<PersonViewModel>(person);

            Assert.That(viewModel.FirstName, Is.EqualTo(person.FirstName));
            Assert.That(viewModel.LastName, Is.EqualTo(person.LastName));
        }
    }
}