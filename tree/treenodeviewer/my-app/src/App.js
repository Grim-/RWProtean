import React, { useState } from 'react';
import NodeEditor from './NodeEditor';
import DefEditor from './components/DefEditor';
import Button from './components/Button';

function App() {
  const [activeTab, setActiveTab] = useState('nodes');

  return (
    <div className="App">
      <div className="bg-white border-b p-4">
        <div className="flex gap-2">
          <Button
            onClick={() => setActiveTab('nodes')}
            className={`${activeTab === 'nodes' ? 'bg-blue-500 text-white' : 'bg-gray-200'}`}
          >
            Node Editor
          </Button>
          <Button
            onClick={() => setActiveTab('defs')}
            className={`${activeTab === 'defs' ? 'bg-blue-500 text-white' : 'bg-gray-200'}`}
          >
            Def Editor
          </Button>
        </div>
      </div>

      <div className="p-4">
        {activeTab === 'nodes' ? <NodeEditor /> : <DefEditor />}
      </div>
    </div>
  );
}

export default App;
