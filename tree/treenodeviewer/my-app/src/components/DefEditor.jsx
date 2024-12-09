import React, { useState } from 'react';
import { DefTypes, DefStructures } from '../DefTypes';

const DefEditor = ({ nodes, setNodes, paths, setPaths }) => {
  const [selectedType, setSelectedType] = useState(null);
  const [currentDef, setCurrentDef] = useState(null);

  const handleTypeChange = (type) => {
    setSelectedType(type);
    setCurrentDef({
      defName: '',
      type: type,
      ...createEmptyDefForType(type)
    });
  };

  const createEmptyDefForType = (type) => {
    const structure = DefStructures[type];
    const emptyDef = {};

    Object.keys(structure.properties).forEach(prop => {
      const propDef = structure.properties[prop];
      if (propDef === 'string') emptyDef[prop] = '';
      else if (propDef === 'number') emptyDef[prop] = 0;
      else if (propDef === 'boolean') emptyDef[prop] = false;
      else if (propDef === 'defList') emptyDef[prop] = [];
      else if (propDef.type === 'list') emptyDef[prop] = [];
      else if (propDef.type === 'vector2') emptyDef[prop] = { x: 0, y: 0 };
      else if (propDef.type === 'enum') emptyDef[prop] = propDef.values[0];
    });

    return emptyDef;
  };

  const renderPropertyEditor = (propName, propDef, value, onChange) => {
    if (typeof propDef === 'string') {
      switch (propDef) {
        case 'string':
          return (
            <input
              type="text"
              value={value || ''}
              onChange={e => onChange(e.target.value)}
              className="w-full p-2 border rounded"
            />
          );
        case 'number':
          return (
            <input
              type="number"
              value={value || 0}
              onChange={e => onChange(Number(e.target.value))}
              className="w-full p-2 border rounded"
            />
          );
        case 'defList':
          return (
            <div>
              {(value || []).map((item, idx) => (
                <div key={idx} className="flex gap-2 mb-2">
                  <input
                    type="text"
                    value={item}
                    onChange={e => {
                      const newList = [...value];
                      newList[idx] = e.target.value;
                      onChange(newList);
                    }}
                    className="flex-1 p-2 border rounded"
                  />
                  <button
                    onClick={() => {
                      const newList = value.filter((_, i) => i !== idx);
                      onChange(newList);
                    }}
                    className="px-2 py-1 bg-red-500 text-white rounded"
                  >
                    ×
                  </button>
                </div>
              ))}
              <button
                onClick={() => onChange([...value, ''])}
                className="w-full p-2 bg-blue-500 text-white rounded"
              >
                Add Item
              </button>
            </div>
          );
      }
    } else if (propDef.type === 'enum') {
      return (
        <select
          value={value}
          onChange={e => onChange(e.target.value)}
          className="w-full p-2 border rounded"
        >
          {propDef.values.map(val => (
            <option key={val} value={val}>{val}</option>
          ))}
        </select>
      );
    }
  };

  return (
    <div className="p-4">
      <div className="mb-4">
        <select
          value={selectedType || ''}
          onChange={e => handleTypeChange(e.target.value)}
          className="w-full p-2 border rounded"
        >
          <option value="">Select Def Type</option>
          {Object.entries(DefTypes).map(([key, value]) => (
            <option key={value} value={value}>{value}</option>
          ))}
        </select>
      </div>

      {selectedType && currentDef && (
        <div className="space-y-4">
          <div>
            <label className="block text-sm font-medium mb-1">Def Name</label>
            <input
              type="text"
              value={currentDef.defName}
              onChange={e => setCurrentDef({ ...currentDef, defName: e.target.value })}
              className="w-full p-2 border rounded"
            />
          </div>

          {Object.entries(DefStructures[selectedType].properties).map(([propName, propDef]) => (
            <div key={propName}>
              <label className="block text-sm font-medium mb-1">{propName}</label>
              {renderPropertyEditor(
                propName,
                propDef,
                currentDef[propName],
                value => setCurrentDef({ ...currentDef, [propName]: value })
              )}
            </div>
          ))}
        </div>
      )}
    </div>
  );
};

export default DefEditor;
