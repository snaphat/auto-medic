using System;
class Program
{
    int a;
    public Program()
    {
        a = 0;
    }
    int get_foo()
    {
        return 0;
    }

    void set_foo(int a)
    {
        this.a = a;
    }



    static int Main(string[] args)
    {
        dynamic program = new Program();
        dynamic a = program.get_foo();
        if (a == 1)
        {
            Console.WriteLine("A equal 1!\n");
        }
        else
        {
            Console.WriteLine("A equal 0!\n");
        }

        return 0;
    }
}
