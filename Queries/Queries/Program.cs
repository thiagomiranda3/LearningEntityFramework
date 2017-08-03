using System;
using System.Linq;
using System.Data.Entity;

namespace Queries
{
    class Program
    {
        static void Main(string[] args)
        {
            var context = new PlutoContext();

            // Adicionando dados ao banco
            var author = context.Authors.Single(a => a.Id == 1);

            var course = new Course
            {
                Name = "New Course",
                Description = "New Description",
                FullPrice = 19.95f,
                Level = 1,
                AuthorId = 1, // Passando apenas o Id já é suficiente para o EF associá-lo ao objeto certo no banco
                Author = author // Em aplicações desktop, podemos passar tb apenas o objeto. Mas precisamos de outra query para pegar o author no banco, caso não esteja na memória
            };

            context.Courses.Add(course);
            context.SaveChanges();

            // Alterando dados no banco

            // Primeira Forma - Buscando o objeto no banco e alterando os campos necessários
            var courseEdit = context.Courses.Find(1); // Busca pela chave primária, versão mais simples que Single
            courseEdit.Name = "NewName";
            courseEdit.AuthorId = 2;

            context.SaveChanges();

            // Segunda Forma - Criando um objeto novo com todos os campos e atualizando-o direto no banco
            var courseEdit2 = new Course
            {
                Id = 1,
                Name = "NewName",
                Description = "New Description",
                FullPrice = 19.95f,
                Level = 1,
                AuthorId = 2
            };

            context.Entry(courseEdit2).State = EntityState.Modified;
            context.SaveChanges();

            // Deletando dados no banco

            var courseDelete = context.Courses.Find(1);
            context.Courses.Remove(courseDelete);
            context.SaveChanges();
        }

        static void LazyEagerLoading()
        {
            var context = new PlutoContext();

            // Query com Lazy Loading para Author
            var course = context.Courses
                .OrderBy(c => c.Level == 1)
                .First(c => c.FullPrice > 10);

            Console.WriteLine(course.Name);
            Console.WriteLine(course.Author.Name);

            // Problema N + 1 quando utilizamos o Lazy Loading
            var courses = context.Courses;

            // Cada requisição ao Author causa uma nova requisição ao banco, pois ele não foi carregado previamente
            foreach (var c in courses)
                Console.WriteLine("{0} {1}", c.Name, c.Author.Name);

            // Resolvendo o problema do N + 1 com o Eager Loading usando o médoto Include com Lambdas (Best Practice)
            var courses2 = context.Courses.Include(c => c.Author);

            // Utilizando o Explicit Load. Serve para buscar uma grande quantidade de dados em tabelas diferentes
            // com mais de uma query ao banco, ao invés de usar uma grande quantidade de joins como no Eager Loading.
            var author = context.Authors.Single(a => a.Id == 1);

            // MSDN Way
            context.Entry(author)
                .Collection(a => a.Courses)
                .Query()
                .Where(c => c.FullPrice == 0)
                .Load();

            // Mosh Way
            context.Courses.Where(c => c.AuthorId == author.Id && c.FullPrice == 0).Load();

            // Explicid Load com lista
            var authors = context.Authors.ToList();
            var authorIds = authors.Select(a => a.Id);
            context.Courses.Where(c => authorIds.Contains(c.AuthorId) && c.FullPrice == 0).Load();
        }

        static void LinqSyntax()
        {
            var context = new PlutoContext();

            // Cursos que tem c# no nome
            var query = from c in context.Courses
                        where c.Name.Contains("c#")
                        orderby c.Name
                        select c;

            // Cursos feitos pelo autor de id 1, ordenado em ordem descendente por Level
            var query1 = from c in context.Courses
                         where c.Author.Id == 1
                         orderby c.Level descending, c.Name
                         select new { Name = c.Name, Author = c.Author.Name };

            // Divide os cursos num array agrupado por Level na mesma tabela
            var query2 = from c in context.Courses
                         group c by c.Level
                         into g
                         select g;

            // Uso do join. Neste caso, onde Course tem o objeto Author, não é necessário. Mas ai abaixo tem um exemplo de como seria
            var query3 = from c in context.Courses
                         join a in context.Authors on c.AuthorId equals a.Id
                         select new { Name = c.Name, Author = a.Name };

            // Group Join. Agrupa duas tabelas diferentes. Agrupa cursos feitos por cada autor
            var query4 = from a in context.Authors
                         join c in context.Courses on a.Id equals c.AuthorId
                         into g
                         select new { Author = a.Name, Courses = g };

            var query5 = from a in context.Authors
                         from c in context.Courses
                         select new { Author = a.Name, Course = c.Name };

            // Foreach da Query2
            foreach (var group in query2)
            {
                Console.WriteLine("{0} ({1})", group.Key, group.Count());

                foreach (var course in group)
                    Console.WriteLine("\t{0}", course.Name);
            }

            // Foreach da Query4
            foreach (var group in query4)
                Console.WriteLine("{0} ({1})", group.Author, group.Courses.Count());

            // Foreach da Query5
            foreach (var cross in query5)
                Console.WriteLine("{0} {1}", cross.Author, cross.Course);

            Console.ReadLine();
        }

        static void LinqExtensionMethods()
        {
            var context = new PlutoContext();

            // Seleção de cursos com Level igual a 1
            var query = context.Courses
                .Where(c => c.Level == 1)
                .Select(c => new { Name = c.Name, Author = c.Author.Name });

            // Seleção das tags baseadas nos cursos com Level igual a 1 e ordenadas de forma descendente
            var query2 = context.Courses
                .Where(c => c.Level == 1)
                .OrderByDescending(c => c.Level)
                .ThenBy(c => c.Name)
                .Select(c => c.Tags);

            // Mesma função da query2, mas que não necessita de dois foreach. Não mostra objetos iguais com Distinct
            var query3 = context.Courses
                .Where(c => c.Level == 1)
                .OrderByDescending(c => c.Level)
                .ThenBy(c => c.Name)
                .SelectMany(c => c.Tags)
                .Distinct();

            // Divide os cursos num array agrupado por Level na mesma tabela
            var query4 = context.Courses.GroupBy(c => c.Level);

            // Exemplo de um join comum, caso tivesse que ser feito
            var query5 = context.Courses
                .Join(context.Authors,
                    c => c.AuthorId,
                    a => a.Id,
                    (course, author) => new
                    {
                        Course = course.Name,
                        Author = author.Name
                    });

            // Group join. Conta a quantidade de cursos por autor 
            var query6 = context.Authors
                .GroupJoin(context.Courses,
                a => a.Id,
                c => c.AuthorId,
                (author, course) => new
                {
                    Author = author.Name,
                    Courses = course.Count()
                });

            // Cross Join. Seleciona todas as tabelas de Author e Course
            var query7 = context.Authors.SelectMany(a => context.Courses, (author, course) => new
            {
                Author = author.Name,
                Course = course.Name
            });

            // #### Querys que só existem no Linq por Extension Method ###

            // Pula os primeiros 10 cursos e pega os próximos 10
            var query8 = context.Courses.Skip(10).Take(10);

            // Retorna o primeiro da tabela. Se estiver vazia retorna um excessão
            var query9 = context.Courses.First();
            // O primeiro elemento que possui preço maior que 100
            var query9a = context.Courses.First(c => c.FullPrice > 100);

            // Se estiver vazia, retorna null. Também aceita sobrecargas
            var query10 = context.Courses.FirstOrDefault();

            // Não funciona com bancos relacionais. Retorna o último elemento da lista com os sem predicado
            var query11 = context.Courses.Last();
            var query11a = context.Courses.LastOrDefault();

            // Se houver mais de um elemento com o predicado colocado, haverá uma excessão. Só pode haver um item com o predicado definido
            var query12 = context.Courses.Single(c => c.Id == 1);
            var query12a = context.Courses.SingleOrDefault(c => c.Id == 1);

            // Retorna verdadeiro ou falso caso todos os elementos da lista satisfaça a condição
            bool boolQuery = context.Courses.All(c => c.FullPrice > 10);

            // Retorna verdadeiro ou false caso algum elemento da lista satisfaça a condição
            bool boolQuery2 = context.Courses.Any(c => c.Level == 1);

            // Retorna a quantidade de elementos numa lista ou tabela. Também aceita condições
            int countQuery = context.Courses.Count();
            int countQuery1 = context.Courses.Count(c => c.Level == 1);

            // Retorna o valor máxima, mínimo e média de acordo com as condições. Pode ou não ter condições
            var countQuery2 = context.Courses.Max(c => c.FullPrice);
            var countQuery3 = context.Courses.Min(c => c.FullPrice);
            var countQuery4 = context.Courses.Average(c => c.FullPrice);

            // Foreach da Query2
            foreach (var group in query2)
            {
                foreach (var course in group)
                    Console.WriteLine("\t{0} {1}", course.Id, course.Name);
            }

            foreach (var tags in query3)
            {
                Console.WriteLine("\t{0} {1}", tags.Id, tags.Name);
            }

            Console.ReadLine();
        }
    }
}
