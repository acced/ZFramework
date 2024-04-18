# Asynchronous Programming



## Tasks in C# Asynchronous Programming

What is a Task? Let me give a single-line answer: "A Task is a basic unit of the Task Parallel Library (TPL)".

On a basic level,a task is nothing but a unit of work.Let's try to map them with real-life tasks to understand them better.


* A task can run/start: Real-life task can run/start(read proceed)
* A task can wait:Real-life tasks wait too
* A task can cancel:No need to provide an example
* A task can have a child Task:There are subtasks in people's lives.

  Let's try one small example to understand Tasks.



```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asynchronious    {
    class Program
    {
        public static void Main(String [] args)
        {
            Task t = new Task(
                () => {
                       System.Threading.Thread.Sleep(5000);
                       Console.WriteLine("Huge Task Finish");
                     }
                );

            //Start the Task
            t.Start();
            //Wait for finish the Task
            t.Wait();
            Console.ReadLine();
        }
    }
}
```

We are calling the Start() method to start the Task. After that, we are calling the Wait() method that implies we are waiting for the task to finish.
