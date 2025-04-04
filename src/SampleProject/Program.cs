using SampleProject;

using (LogWriter lw = new LogWriter(@"d:\test.txt")) {
    lw.Write("Test1");
}

using (LogWriter lw = new LogWriter(@"d:\test.txt")) {
    lw.Write("Test2");
}

await using (LogWriter lw = new LogWriter(@"d:\test.txt")) {
    lw.Write("Test3");
}

await using (LogWriter lw = new LogWriter(@"d:\test.txt")) {
    lw.Write("Test4");
}


Console.WriteLine(6);
