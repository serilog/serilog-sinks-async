using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Serilog.Sinks.Async.UnitTests
{
    public static class Assert
    {
        public static async Task<TException> ThrowsAsync<TException>(Func<Task> action) where TException : Exception
        {
            try
            {
                await action();
            }
            catch (TException ex)
            {
                return ex;
            }
            catch (Exception ex)
            {
                throw new AssertFailedException(string.Format("Was expecting an exception of type {0}, but threw {1}",
                    typeof (TException), ex.GetType()));
            }

            throw new AssertFailedException(string.Format("Was expecting an exception of type {0}", typeof (TException)));
        }

        public static void AreEqual(int expected, int actual)
        {
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(expected, actual);
        }

        public static void IsTrue(bool actual)
        {
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(actual);
        }
    }
}