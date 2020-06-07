using Raven.Abstractions.Connection;
using Raven.Abstractions.Exceptions;
using Raven.Client;
using Raven.Client.Document;
using Raven.Client.UniqueConstraints;
using System;

namespace RavenDbTests
{
    class Program
    {
        const string product1Id = "b5d19a6c-821f-4286-b90f-3c9fcef3d5e4";
        const string product2Id = "1a2aece7-08d6-4e15-ac06-f5647a34d17f";
        static void Main(string[] args)
        {
            {
                Console.WriteLine("RavenDb Test program started.");

                SimulateErrorOnInserting();
                SimulateErrorOnUpdating();
                SimulateCheckingUniqueConstraintsBeforeInserting();

                Console.WriteLine("Press anykey to exit.");
                Console.ReadKey();
            }

            void SimulateErrorOnInserting()
            {
                Console.WriteLine("Simulate Error On Inserting...");

                using (IDocumentSession session = DocumentStoreHolder.Store.OpenSession())
                {
                    // insert Product 1
                    var product1 = new Product
                    {
                        ProductId = new Guid(product1Id),
                        ProductName = "Laptop",
                        Gtin = "ABC12345"
                    };
                    session.Store(product1, product1.ProductId.ToString());

                    // insert Product 2 with same Gtin
                    var product2 = new Product
                    {
                        ProductId = new Guid(product2Id),
                        ProductName = "Monitor",
                        Gtin = "ABC12345"
                    };
                    session.Store(product2, product2.ProductId.ToString());

                    try
                    {
                        session.SaveChanges();
                    }
                    catch (ErrorResponseException ex)
                    {
                        if (ex.Message.Contains("OperationVetoedException") && ex.Message.Contains("UniqueConstraintsPutTrigger"))
                            Console.WriteLine($"Product Gtin is not unique.");
                        else
                            Console.WriteLine(ex.Message);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }

                Console.WriteLine(new string('-', 50));
            }

            void SimulateErrorOnUpdating()
            {
                Console.WriteLine("Simulate Error On Updating...");

                using (IDocumentSession session = DocumentStoreHolder.Store.OpenSession())
                {
                    // insert Product 1
                    var product1 = new Product
                    {
                        ProductId = new Guid(product1Id),
                        ProductName = "Laptop",
                        Gtin = "ABC12345"
                    };
                    session.Store(product1, product1.ProductId.ToString());

                    // insert Product 2 with different Gtin
                    var product2 = new Product
                    {
                        ProductId = new Guid(product2Id),
                        ProductName = "Monitor",
                        Gtin = "XYZ12345"
                    };
                    session.Store(product2, product2.ProductId.ToString());

                    // update Product2 Gtin with Product1 Gtin
                    var product = session.Load<Product>(product2Id);
                    product.Gtin = "ABC12345";

                    try
                    {
                        session.SaveChanges();
                    }
                    catch (ErrorResponseException ex)
                    {
                        if (ex.Message.Contains("OperationVetoedException") && ex.Message.Contains("UniqueConstraintsPutTrigger"))
                            Console.WriteLine($"Product Gtin is not unique.");
                        else
                            Console.WriteLine(ex.Message);
                    }
                }

                Console.WriteLine(new string('-', 50));
            }

            void SimulateCheckingUniqueConstraintsBeforeInserting()
            {
                Console.WriteLine("Simulate Checking Unique Constraints Before Inserting...");

                using (IDocumentSession session = DocumentStoreHolder.Store.OpenSession())
                {
                    // insert Product 1
                    var product1 = new Product
                    {
                        ProductId = new Guid(product1Id),
                        ProductName = "Laptop",
                        Gtin = "ABC12345"
                    };

                    var checkResult = session.CheckForUniqueConstraints(product1);

                    // insert only if there are no unique constraints
                    if (checkResult.ConstraintsAreFree())
                    {
                        session.Store(product1, product1.ProductId.ToString());
                        session.SaveChanges();
                    }
                }

                using (IDocumentSession session = DocumentStoreHolder.Store.OpenSession())
                {
                    // insert Product 2 with same Gtin
                    var product2 = new Product
                    {
                        ProductId = new Guid(product2Id),
                        ProductName = "Monitor",
                        Gtin = "ABC12345"
                    };

                    var checkResult = session.CheckForUniqueConstraints(product2);

                    // insert only if there are no unique constraints
                    if (checkResult.ConstraintsAreFree())
                    {
                        session.Store(product2);
                    }
                    else
                    {
                        var existingProduct = checkResult.DocumentForProperty(x => x.Gtin);

                        Console.WriteLine($"Gtin value: {product2.Gtin} belongs to product id: {existingProduct.ProductId}");
                    }
                }

                Console.WriteLine(new string('-', 50));
            }
        }
    }

    static class DocumentStoreHolder
    {
        private static readonly Lazy<IDocumentStore> LazyStore =
            new Lazy<IDocumentStore>(() =>
            {
                var store = new DocumentStore
                {
                    Url = "http://localhost:8080",
                    DefaultDatabase = "TestDb"
                }
                .RegisterListener(new UniqueConstraintsStoreListener())
                .Initialize();

                return store;
            });

        public static IDocumentStore Store => LazyStore.Value;
    }

    public class Product
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        [UniqueConstraint]
        public string Gtin { get; set; }
    }
}
