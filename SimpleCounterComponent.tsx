import React, { useState } from 'react';
import { Plus, Minus, RotateCcw } from 'lucide-react';
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert';

const SimpleCounter = () => {
    const [count, setCount] = useState(0);

    return (
        <div className="w-full max-w-lg mx-auto p-8">
            <Alert className="mb-6">
                <AlertTitle>Simple Counter Demo</AlertTitle>
                <AlertDescription>
                    A basic counter to test React component functionality.
                </AlertDescription>
            </Alert>
            
            <div className="bg-gray-800 rounded-lg p-8 shadow-lg">
                <h1 className="text-2xl font-bold text-white mb-6 text-center">Counter</h1>
                
                <div className="text-6xl text-white text-center p-8 bg-gray-700 rounded-lg mb-6">
                    {count}
                </div>
                
                <div className="flex gap-4 justify-center">
                    <button
                        onClick={() => setCount(c => c - 1)}
                        className="flex items-center gap-2 px-6 py-3 bg-red-600 text-white rounded-lg hover:bg-red-700"
                    >
                        <Minus size={20} />
                        Decrease
                    </button>
                    
                    <button
                        onClick={() => setCount(0)}
                        className="flex items-center gap-2 px-6 py-3 bg-gray-600 text-white rounded-lg hover:bg-gray-700"
                    >
                        <RotateCcw size={20} />
                        Reset
                    </button>
                    
                    <button
                        onClick={() => setCount(c => c + 1)}
                        className="flex items-center gap-2 px-6 py-3 bg-blue-600 text-white rounded-lg hover:bg-blue-700"
                    >
                        <Plus size={20} />
                        Increase
                    </button>
                </div>
            </div>
        </div>
    );
};

export default SimpleCounter;