﻿using UnityEngine;
using UnityEditor;
using clojure.lang;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Threading;

public class AsyncReplWindow : EditorWindow {
  struct CodeAndSocket {
    public string code;
    public Socket socket;

    public CodeAndSocket(string code, Socket socket) {
      this.code = code;
      this.socket = socket;
    }
  }

  string output = "Clojure Async REPL v0.1 (sexant)\n";
  Queue<CodeAndSocket> incomingLines = new Queue<CodeAndSocket>();
  AsynchronousSocketListener listener;
  Thread thread;

  [MenuItem ("Window/Clojure Async REPL")]
  static void Init () {
    AsyncReplWindow window = (AsyncReplWindow)EditorWindow.GetWindow (typeof (AsyncReplWindow));
    window.StartListening();
  }

  public void StartListening() {
    RT.load("unityRepl");
    listener = new AsynchronousSocketListener();
    listener.OnGetData += GetData;
    thread = new Thread(() => listener.StartListening());
    thread.Start();
  }

  void GetData(string code, int length, StateObject state) {
    incomingLines.Enqueue(new CodeAndSocket(code, state.workSocket));
  }

  void OnDestroy() {
    listener.OnGetData -= GetData;
    listener.StopListening();
    thread.Join();
  }

  void OnGUI () {
    GUILayout.TextArea(output, 500);
  }

  void Update() {
    while(incomingLines.Count > 0) {
      CodeAndSocket cas = incomingLines.Dequeue();
      string line = cas.code;
      Socket socket = cas.socket;

      var result = RT.var("unityRepl", "repl-eval-string").invoke(line);
      Debug.Log(result);
      if(result != null)
        listener.Send(socket, result.ToString() + "\x04");
      else
        listener.Send(socket, "nil\x04");

      output += line + "\n";      
    }
  }
}
