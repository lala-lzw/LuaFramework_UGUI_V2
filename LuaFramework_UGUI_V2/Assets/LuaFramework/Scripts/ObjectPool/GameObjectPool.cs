using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace LuaFramework {

	[Serializable]
	public class PoolInfo {
		public string poolName;
		public GameObject prefab;
		public int poolSize;
		public bool fixedSize;
	}
	/// <summary>
	/// maxSize :指最大对象个数
	/// poolSize：指当前池创建了多少个对象
	/// poolname：名字
	/// poolRoot: 父物体transform指你要挂载到那个物体上面
	/// poolobjPre: 物体预制体
	/// availableobjStack: 物体池  
	/// </summary>
	public class GameObjectPool {
        private int maxSize;
		private int poolSize;
		private string poolName;
        private Transform poolRoot;
        private GameObject poolObjectPrefab;
        private Stack<GameObject> availableObjStack = new Stack<GameObject>();
		/// <summary>
		/// maxSize :指最大对象个数
		/// poolSize：指当前池创建了多少个对象
		/// poolname：名字
		/// poolRoot: 父物体transform指你要挂载到那个物体上面
		/// poolobjPre: 物体预制体
		/// availableobjStack: 物体池  
		/// </summary>
		public GameObjectPool(string poolName, GameObject poolObjectPrefab, int initCount, int maxSize, Transform pool) {
			this.poolName = poolName;
			this.poolSize = initCount;
            this.maxSize = maxSize;
            this.poolRoot = pool;
            this.poolObjectPrefab = poolObjectPrefab;

			//populate the pool
			for(int index = 0; index < initCount; index++) {
				AddObjectToPool(NewObjectInstance());
			}
		}

		//o(1)
        private void AddObjectToPool(GameObject go) {
			//add to pool
            go.SetActive(false);
            availableObjStack.Push(go);
            go.transform.SetParent(poolRoot, false);
		}

        private GameObject NewObjectInstance() {
            return GameObject.Instantiate(poolObjectPrefab) as GameObject;
		}

		public GameObject NextAvailableObject() {
            GameObject go = null;
			if(availableObjStack.Count > 0) {
				go = availableObjStack.Pop();
			} else {
				Debug.LogWarning("No object available & cannot grow pool: " + poolName);
			}
            go.SetActive(true);
            return go;
		} 
		
		//o(1)
        public void ReturnObjectToPool(string pool, GameObject po) {
            if (poolName.Equals(pool)) {
                AddObjectToPool(po);
			} else {
				Debug.LogError(string.Format("Trying to add object to incorrect pool {0} ", poolName));
			}
		}
	}
}
