import React, { useState } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { AlertCircle, Play, Trash2 } from 'lucide-react';

const CodePlayground = () => {
  const [code, setCode] = useState('// Write your JavaScript code here\nconsole.log("Hello World!");');
  const [output, setOutput] = useState([]);
  const [error, setError] = useState(null);

  // Custom console implementation
  const customConsole = {
    log: (...args) => {
      setOutput(prev => [...prev, { type: 'log', content: args.join(' ') }]);
    },
    error: (...args) => {
      setOutput(prev => [...prev, { type: 'error', content: args.join(' ') }]);
    },
    warn: (...args) => {
      setOutput(prev => [...prev, { type: 'warn', content: args.join(' ') }]);
    },
    clear: () => {
      setOutput([]);
    }
  };

  const runCode = () => {
    setOutput([]);
    setError(null);

    try {
      // Create a safe execution environment
      const safeEval = (code) => {
        // Create a new Function with custom console
        const func = new Function('console', code);
        // Execute the function with our custom console
        func(customConsole);
      };

      safeEval(code);
    } catch (err) {
      setError(err.message);
    }
  };

  const clearOutput = () => {
    setOutput([]);
    setError(null);
  };

  return (
    <Card className="w-full max-w-4xl mx-auto">
      <CardHeader>
        <CardTitle className="flex items-center justify-between">
          <span>JavaScript Playground</span>
          <div className="space-x-2">
            <button
              onClick={runCode}
              className="px-4 py-2 bg-green-600 text-white rounded-md hover:bg-green-700 flex items-center"
            >
              <Play className="w-4 h-4 mr-2" />
              Run
            </button>
            <button
              onClick={clearOutput}
              className="px-4 py-2 bg-gray-600 text-white rounded-md hover:bg-gray-700 flex items-center"
            >
              <Trash2 className="w-4 h-4 mr-2" />
              Clear
            </button>
          </div>
        </CardTitle>
      </CardHeader>
      <CardContent>
        <div className="grid grid-cols-1 gap-4">
          {/* Code Editor */}
          <div className="relative">
            <textarea
              value={code}
              onChange={(e) => setCode(e.target.value)}
              className="w-full h-64 p-4 font-mono text-sm bg-gray-900 text-gray-100 rounded-md resize-none"
              spellCheck="false"
            />
          </div>

          {/* Output Console */}
          <div className="bg-gray-900 rounded-md p-4 h-48 overflow-auto">
            <div className="font-mono text-sm">
              {error && (
                <div className="flex items-start text-red-500 mb-2">
                  <AlertCircle className="w-4 h-4 mr-2 mt-1 flex-shrink-0" />
                  <span>{error}</span>
                </div>
              )}
              {output.map((item, index) => (
                <div
                  key={index}
                  className={`mb-1 ${
                    item.type === 'error'
                      ? 'text-red-500'
                      : item.type === 'warn'
                      ? 'text-yellow-500'
                      : 'text-green-500'
                  }`}
                >
                  {item.content}
                </div>
              ))}
            </div>
          </div>
        </div>
      </CardContent>
    </Card>
  );
};

export default CodePlayground;