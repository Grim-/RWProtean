// utils/xmlSerializer.js - New file for XML handling
export const serializeDefToXml = (def) => {
  let xml = `  <${def.type}>\n`;
  xml += `    <defName>${def.defName}</defName>\n`;

  Object.entries(def).forEach(([key, value]) => {
    if (key !== 'type' && key !== 'defName') {
      xml += serializeProperty(key, value, 2);
    }
  });

  xml += `  </${def.type}>\n\n`;
  return xml;
};

const serializeProperty = (key, value, indent) => {
  const spaces = ' '.repeat(indent * 2);

  if (Array.isArray(value)) {
    if (value.length === 0) return '';
    let xml = `${spaces}<${key}>\n`;
    value.forEach(item => {
      if (typeof item === 'object') {
        xml += `${spaces}  <li>\n`;
        Object.entries(item).forEach(([k, v]) => {
          xml += serializeProperty(k, v, indent + 2);
        });
        xml += `${spaces}  </li>\n`;
      } else {
        xml += `${spaces}  <li>${item}</li>\n`;
      }
    });
    xml += `${spaces}</${key}>\n`;
    return xml;
  }

  if (typeof value === 'object' && value !== null) {
    let xml = `${spaces}<${key}>\n`;
    Object.entries(value).forEach(([k, v]) => {
      xml += serializeProperty(k, v, indent + 1);
    });
    xml += `${spaces}</${key}>\n`;
    return xml;
  }

  if (value !== undefined && value !== '') {
    return `${spaces}<${key}>${value}</${key}>\n`;
  }

  return '';
};
