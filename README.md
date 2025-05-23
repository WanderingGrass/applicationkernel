# applicationkernel

<p align="center">
<h3 align="center">ApplicationKernel</h3>
  <p align="center">
    一个快速构建应用程序的项目（学习版）
    <br />
    <a href="https://github.com/WanderingGrass/applicationkernel/issues">报告Bug</a>
    ·
    <a href="https://github.com/WanderingGrass/applicationkernel/issues">提出新特性</a>
  </p>

</p>



## 目录

- [applicationkernel](#applicationkernel)
  - [目录](#目录)
    - [上手指南](#上手指南)
      - [开发前的配置要求](#开发前的配置要求)
      - [**安装步骤**](#安装步骤)
    - [开发的架构](#开发的架构)
    - [部署](#部署)
    - [使用到的框架](#使用到的框架)

### 上手指南

### 构思

本项目是一组构建分布式框架工具（目前正在开发中，预计2025完成基础核心框架）

无论开发任何业务,随着业务的增长,系统复杂度增高.高并发,高可用就是必须要保证的,无论使用微服务/事件驱动架构/Actor模型等方案,分布式系统都存在相同的问题,计算的分布,计算之间的协调,伸缩性,容错性,可扩展性.

如：在开发一款web服务端时
    1：设计结构一般是多进程->单数据库,通过nginx进行负载均衡.随着业务的快速增长，DB的压力就会很大,并且多个应用程序的各自DB实例访问DB同一行或同一个key，可能会发生并发问题，这时候就需要通过事务,乐观锁悲观锁等方式解决。同时为了防止访问数据库速度慢并减少读负载，一般就是再加一层缓存.
    2：缓存适用不易改变，频繁读取的数据。但是同时也带来了其他问题，如缓存击穿，缓存穿透，缓存雪崩，缓存一致性等问题.
    3：一般会增加消息队列来解决这个问题,同时又是异步的。当我们修改某个值的时候就推送到队列中.一些应用的work进程就会监听队列,然后进行处理.但是最初的一个程序就可解决，随着增长，现在需要经历3层或以上的程序才能到达。


所以,本项目的目的就是为了解决这些问题,提供一个快速构建分布式系统的框架,让开发者可以快速构建分布式系统,而不需要关心这些问题.
主要挑战点（分布式系统概念与设计）：

    - 异构型：系统,硬件,编程语言等
    - 开放性：不同方式被扩展和重新实现特性.
    - 安全性：加密方式
    - 可伸缩性：资源数量和用户数量激增,仍可以保证有效性
    - 故障处理：在产生不正确结果活着它们完成应该进行的计算之前就停止了.
    - 并发性：服务和应用均提供可被客户共享的资源
    - 透明性：
    - 服务质量：

###### 开发前的配置要求

###### **安装步骤**

```sh
git clone https://github.com/WanderingGrass/applicationkernel.git
```

### 开发的架构
本项目是基于Actor模型进行开发，当然也包含分布式的其他组件.
### 基本概念
#### Actor:
    Actor(状态,行为，消息队列)，不需要考虑低级并发结构。
    核心属性：运行的上下文（生命周期），唯一标识，其他Grain访问对象
    生命周期：激活时，停用时
    消息接收：定时器
#### 核心组件
    基础设施：  
        -序列化组件：
          
    缓存组件：
    
    消息队列：
    服务发现：
    日志组件：
### 使用到的框架
- [RabbitMQ](https://rabbitmq.org.cn/docs)

### Actor开发和其他开发思路不同



