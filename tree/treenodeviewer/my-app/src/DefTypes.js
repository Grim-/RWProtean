export const DefTypes = {
  UPGRADE: 'Protean.UpgradeDef',
  UPGRADE_TREE: 'Protean.UpgradeTreeDef',
  UPGRADE_TREE_NODE: 'Protean.UpgradeTreeNodeDef',
  UPGRADE_PATH: 'Protean.UpgradePathDef'
};

export const DefStructures = {
  'Protean.UpgradeDef': {
    required: ['defName', 'parasiteLevelRequired', 'pointCost'],
    properties: {
      parasiteLevelRequired: 'number',
      prerequisites: 'defList',
      uiIcon: 'string',
      pointCost: 'number',
      hediffEffects: {
        type: 'list',
        of: {
          hediffDef: 'string'
        }
      },
      abilityEffects: {
        type: 'list',
        of: {
          abilities: 'defList'
        }
      },
      organEffects: {
        type: 'list',
        of: {
          targetOrgan: 'string',
          addedOrganHediff: 'string',
          isAddition: 'boolean'
        }
      }
    }
  },
  'Protean.UpgradeTreeDef': {
    required: ['defName', 'nodes', 'dimensions'],
    properties: {
      nodes: 'defList',
      dimensions: {
        type: 'vector2',
        x: 'number',
        y: 'number'
      },
      handlerClass: 'string',
      skin: 'string',
      availablePaths: 'defList',
      displayStrategy: 'string'
    }
  },
  'Protean.UpgradeTreeNodeDef': {
    required: ['defName', 'position', 'type'],
    properties: {
      upgrade: 'string',
      position: {
        type: 'vector2',
        x: 'number',
        y: 'number'
      },
      connections: 'defList',
      type: {
        type: 'enum',
        values: ['Normal', 'Keystone', 'Start', 'Branch']
      },
      path: 'string',
      branchPaths: {
        type: 'list',
        of: {
          path: 'string',
          nodes: 'defList'
        }
      }
    }
  },
  'Protean.UpgradePathDef': {
    required: ['defName'],
    properties: {
      exclusiveWith: 'defList',
      pathDescription: 'string',
      pathUIIcon: 'string'
    }
  }
};
