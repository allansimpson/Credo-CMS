import{i as s,g as p}from"./index-BL7Bold5.js";const c={search:(a,e=1,i=20)=>{const r=new URLSearchParams({q:a,page:String(e),pageSize:String(i)});return p(`/api/public/search?${r}`,{emitUnauthorized:!1})},rebuild:()=>s("/api/admin/search/rebuild")};export{c as searchApi};
//# sourceMappingURL=search-DbKffCxW.js.map
