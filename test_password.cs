using System;
using System.Web.Helpers;

class Test {
    static void Main() {
        string storedHash = "ACOo3n9s2pWomIX/4Q4jVNId0gQuuWNxPU9H81puIWDFvGKhByaMK8xSeg8cLKuzyw==";
        string password = "Branch@123";
        
        bool result = Crypto.VerifyHashedPassword(storedHash, password);
        Console.WriteLine("Verify result: " + result);
    }
}
