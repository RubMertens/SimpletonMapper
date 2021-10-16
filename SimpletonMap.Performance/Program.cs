using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using SimpletonMap.SourceGenerator;
using SimpletonMap.V4;
using SimpletonMap.V5;

namespace SimpletonMap.Performance
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<MapperPerformanceTests>();
        }
    }

    public class MapperPerformanceTests
    {
        private SimpletonMapper _reflectionMapper;
        private SimpletonMapper _roslynMapper;
        private SimpletonMapper _ilMapper;
        private Person _fromPerson;
        
        public MapperPerformanceTests()
        {
            _reflectionMapper = new SimpletonMapper(MappingStrategy.Reflection);
            _reflectionMapper.Register<Person, PersonViewModel>()
                .With(p => p.MiddleName, p => p.OptionalName);
            _reflectionMapper.Build();
            
            _roslynMapper = new SimpletonMapper(MappingStrategy.Roslyn);
            _roslynMapper.Register<Person, PersonViewModel>()
                .With(p => p.MiddleName, p => p.OptionalName);
            _roslynMapper.Build();

            _ilMapper = new SimpletonMapper(MappingStrategy.Roslyn);
            _ilMapper.Register<Person, PersonViewModel>()
                .With(p => p.MiddleName, p => p.OptionalName);
            _ilMapper.Build();
            
            _fromPerson = new Person()
            {
                FirstName = "Jack",
                LastName = "Thomsen",
                MiddleName = "Percival"
            };
        }

        [Benchmark]
        public void Reflection()
        {
            var personViewModel = _reflectionMapper
                .Map<PersonViewModel>(_fromPerson); 
        }

        [Benchmark]
        public void Roslyn()
        {
            var personViewModel = _roslynMapper
                .Map<PersonViewModel>(_fromPerson);
        }

        [Benchmark]
        public void Il()
        {
            var personViewModel = _ilMapper
                .Map<PersonViewModel>(_fromPerson);
        }

        [Benchmark]
        public void SourceGen()
        {
            var personViewModel = _fromPerson.ToPersonViewModel();
        }
    }
    
    public class Person  {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MiddleName { get; set; }
    }

    [MappedFrom(typeof(Person))]
    public class PersonViewModel
    {
        public string FirstName { get; set; }
        // [MapsFrom(nameof(Person.LastName))]
        public string FamilyName { get; set; }
        public string OptionalName { get; set; }
    }
}